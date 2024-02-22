
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using DeviceMessage = Microsoft.Azure.Devices.Client;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using CloudPillar.Agent.Enums;

namespace CloudPillar.Agent.Handlers;

public class X509DPSProvisioningDeviceClientHandler : IDPSProvisioningDeviceClientHandler
{
    private readonly ILoggerHandler _logger;

    private IDeviceClientWrapper _deviceClientWrapper;
    private readonly IX509CertificateWrapper _X509CertificateWrapper;
    private readonly IProvisioningDeviceClientWrapper _provisioningDeviceClientWrapper;
    private readonly AuthenticationSettings _authenticationSettings;
    private readonly ITwinReportHandler _twinReportHandler;
    public X509DPSProvisioningDeviceClientHandler(
        ILoggerHandler loggerHandler,
        IDeviceClientWrapper deviceClientWrapper,
        IX509CertificateWrapper X509CertificateWrapper,
        IProvisioningDeviceClientWrapper provisioningDeviceClientWrapper,
        IOptions<AuthenticationSettings> options,
        ITwinReportHandler twinReportHandler
)
    {
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _X509CertificateWrapper = X509CertificateWrapper ?? throw new ArgumentNullException(nameof(X509CertificateWrapper));
        _provisioningDeviceClientWrapper = provisioningDeviceClientWrapper ?? throw new ArgumentNullException(nameof(provisioningDeviceClientWrapper));
        _authenticationSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _twinReportHandler = twinReportHandler ?? throw new ArgumentNullException(nameof(twinReportHandler));
    }

    public X509Certificate2? GetCertificate(string deviceId = "")
    {
        using (var store = _X509CertificateWrapper.Open(OpenFlags.ReadOnly, storeLocation: _authenticationSettings.StoreLocation))
        {
            var certificates = _X509CertificateWrapper.GetCertificates(store);
            var filteredCertificate = certificates?.Cast<X509Certificate2>()
               .Where(cert => cert.Subject.StartsWith(ProvisioningConstants.CERTIFICATE_SUBJECT + _authenticationSettings.GetCertificatePrefix())
                && cert.FriendlyName.Contains(ProvisioningConstants.CERTIFICATE_NAME_SEPARATOR)
                && (string.IsNullOrEmpty(deviceId) || cert.FriendlyName.Contains($"{deviceId}{ProvisioningConstants.CERTIFICATE_NAME_SEPARATOR}")))
               .FirstOrDefault();

            return filteredCertificate;
        }
    }

    public async Task<DeviceConnectionResult> InitAuthorizationAsync()
    {
        return await AuthorizationAsync(string.Empty, string.Empty, default, true);
    }

    public async Task<DeviceConnectionResult> AuthorizationDeviceAsync(string XdeviceId, string XSecretKey, CancellationToken cancellationToken, bool checkAuthorization = false)
    {
        return await AuthorizationAsync(XdeviceId, XSecretKey, cancellationToken, false, checkAuthorization);
    }

    private async Task<DeviceConnectionResult> AuthorizationAsync(string XdeviceId, string XSecretKey, CancellationToken cancellationToken, bool IsInitializedLoad = false, bool checkAuthorization = false)
    {
        X509Certificate2? userCertificate = GetCertificate();

        if (userCertificate == null)
        {
            _logger.Error("No certificate found in the store");
            return DeviceConnectionResult.CertificateInvalid;
        }

        var friendlyName = userCertificate?.FriendlyName ?? throw new ArgumentNullException(nameof(userCertificate.FriendlyName));
        var parts = friendlyName.Split(ProvisioningConstants.CERTIFICATE_NAME_SEPARATOR);

        if (parts.Length != 2)
        {
            var error = "The FriendlyName is not in the expected format.";
            _logger.Error(error);
            return DeviceConnectionResult.CertificateInvalid;
        }

        var deviceId = parts[0];
        var iotHubHostName = parts[1];
        var deviceSecret = Encoding.UTF8.GetString(userCertificate.Extensions.First(x => x.Oid?.Value == ProvisioningConstants.DEVICE_SECRET_EXTENSION_KEY).RawData);

        if ((!IsInitializedLoad || checkAuthorization) && !(XdeviceId.Equals(deviceId) && XSecretKey.Equals(deviceSecret)))
        {
            var error = "The deviceId or the SecretKey are incorrect.";
            _logger.Error(error);
            return DeviceConnectionResult.CertificateInvalid;
        }

        if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(iotHubHostName))
        {
            var error = "The deviceId or the iotHubHostName cant be null.";
            _logger.Error(error);
            return DeviceConnectionResult.CertificateInvalid;
        }

        if (IsInitializedLoad)
        {
            _logger.Info($"Try load with the following deviceId: {deviceId}");
        }

        iotHubHostName += ProvisioningConstants.IOT_HUB_NAME_SUFFIX;

        return await InitializeDeviceAsync(deviceId, iotHubHostName, userCertificate, IsInitializedLoad || checkAuthorization, cancellationToken);
    }

    public async Task<DeviceConnectionResult> ProvisioningAsync(string dpsScopeId, X509Certificate2 certificate, string globalDeviceEndpoint, DeviceMessage.Message message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(dpsScopeId);
        ArgumentNullException.ThrowIfNullOrEmpty(globalDeviceEndpoint);
        ArgumentNullException.ThrowIfNull(certificate);

        using var security = _X509CertificateWrapper.GetSecurityProvider(certificate);

        _logger.Debug($"Initializing the device provisioning client...");

        using ProvisioningTransportHandler transport = _deviceClientWrapper.GetProvisioningTransportHandler();

        _logger.Debug($"Initialized for registration Id {security.GetRegistrationID()}.");

        DeviceRegistrationResult result = await _provisioningDeviceClientWrapper.RegisterAsync(globalDeviceEndpoint,
            dpsScopeId,
            security,
            transport);

        if (result == null)
        {
            _logger.Error("RegisterAsync failed");
            return DeviceConnectionResult.CertificateInvalid;
        }

        _logger.Debug($"Registration status: {result.Status}.");
        if (result.Status != ProvisioningRegistrationStatusType.Assigned)
        {
            _logger.Error("Registration status did not assign a hub.");
            return DeviceConnectionResult.CertificateInvalid;
        }
        _logger.Info($"Device {result.DeviceId} registered to {result.AssignedHub}.");

        await OnProvisioningCompleted(message, cancellationToken);

        var devResult = await InitializeDeviceAsync(result.DeviceId, result.AssignedHub, certificate, true, cancellationToken);
        if (devResult == DeviceConnectionResult.Valid)
        {
            await _twinReportHandler.UpdateDeviceCertificateValidity(_authenticationSettings.CertificateExpiredDays, cancellationToken);
        }
        return devResult;
    }

    private async Task<DeviceConnectionResult> InitializeDeviceAsync(string deviceId, string iotHubHostName, X509Certificate2 userCertificate, bool initialize, CancellationToken cancellationToken)
    {
        try
        {
            if (initialize)
            {
                using var auth = _X509CertificateWrapper.GetDeviceAuthentication(deviceId, userCertificate);
                var res = await _deviceClientWrapper.DeviceInitializationAsync(iotHubHostName, auth, cancellationToken);
                if (res != DeviceConnectionResult.Valid)
                {
                    return res;
                }
            }
            return await _deviceClientWrapper.IsDeviceInitializedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception during IoT Hub connection message: {ex.Message}");
            return DeviceConnectionResult.Unknow;
        }
    }

    private async Task OnProvisioningCompleted(DeviceMessage.Message message, CancellationToken cancellationToken)
    {
        //before initialize the device client, we need to complete the message
        try
        {
            if (message != null)
            {
                await _deviceClientWrapper.CompleteAsync(message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"ProvisioningAsync, Complete message failed message: {ex.Message}");
        }
    }
}