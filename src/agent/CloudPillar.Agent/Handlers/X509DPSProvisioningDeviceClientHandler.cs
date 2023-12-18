
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Shared.Entities.Authentication;
using DeviceMessage = Microsoft.Azure.Devices.Client;
using CloudPillar.Agent.Handlers.Logger;
namespace CloudPillar.Agent.Handlers;

public class X509DPSProvisioningDeviceClientHandler : IDPSProvisioningDeviceClientHandler
{
    private readonly ILoggerHandler _logger;

    private IDeviceClientWrapper _deviceClientWrapper;
    private readonly IX509CertificateWrapper _X509CertificateWrapper;
    private readonly IProvisioningDeviceClientWrapper _provisioningDeviceClientWrapper;

    public X509DPSProvisioningDeviceClientHandler(
        ILoggerHandler loggerHandler,
        IDeviceClientWrapper deviceClientWrapper,
        IX509CertificateWrapper X509CertificateWrapper,
        IProvisioningDeviceClientWrapper provisioningDeviceClientWrapper)
    {
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _X509CertificateWrapper = X509CertificateWrapper ?? throw new ArgumentNullException(nameof(X509CertificateWrapper));
        _provisioningDeviceClientWrapper = provisioningDeviceClientWrapper ?? throw new ArgumentNullException(nameof(provisioningDeviceClientWrapper));
    }

    public X509Certificate2? GetCertificate()
    {
        using (var store = _X509CertificateWrapper.Open(OpenFlags.ReadOnly))
        {
            var certificates = _X509CertificateWrapper.GetCertificates(store);
            var filteredCertificate = certificates?.Cast<X509Certificate2>()
               .Where(cert => cert.Subject.StartsWith(ProvisioningConstants.CERTIFICATE_SUBJECT + CertificateConstants.CLOUD_PILLAR_SUBJECT) && cert.FriendlyName.Contains(ProvisioningConstants.CERTIFICATE_NAME_SEPARATOR))
               .FirstOrDefault();

            return filteredCertificate;
        }
    }

    public async Task<bool> InitAuthorizationAsync()
    {
        return await AuthorizationAsync(string.Empty, string.Empty, default, true);
    }

    public async Task<bool> AuthorizationDeviceAsync(string XdeviceId, string XSecretKey, CancellationToken cancellationToken, bool checkAuthorization = false)
    {
        return await AuthorizationAsync(XdeviceId, XSecretKey, cancellationToken, false, checkAuthorization);
    }

    private async Task<bool> AuthorizationAsync(string XdeviceId, string XSecretKey, CancellationToken cancellationToken, bool IsInitializedLoad = false, bool checkAuthorization = false)
    {        
        X509Certificate2? userCertificate = GetCertificate();

        if (userCertificate == null)
        {
            _logger.Error("No certificate found in the store");
            return false;
        }

        var friendlyName = userCertificate?.FriendlyName ?? throw new ArgumentNullException(nameof(userCertificate.FriendlyName));
        var parts = friendlyName.Split(ProvisioningConstants.CERTIFICATE_NAME_SEPARATOR);

        if (parts.Length != 2)
        {
            var error = "The FriendlyName is not in the expected format.";
            _logger.Error(error);
            return false;
        }

        var deviceId = parts[0];
        var iotHubHostName = parts[1];
        var oneMd = Encoding.UTF8.GetString(userCertificate.Extensions.First(x => x.Oid?.Value == ProvisioningConstants.ONE_MD_EXTENTION_KEY).RawData);

        if ((!IsInitializedLoad || checkAuthorization) && !(XdeviceId.Equals(deviceId) && XSecretKey.Equals(oneMd)))
        {
            var error = "The deviceId or the SecretKey are incorrect.";
            _logger.Error(error);
            return false;
        }

        if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(iotHubHostName))
        {
            var error = "The deviceId or the iotHubHostName cant be null.";
            _logger.Error(error);
            return false;
        }

        if (IsInitializedLoad)
        {
            _logger.Info($"Try load with the following deviceId: {deviceId}");
        }

        iotHubHostName += ProvisioningConstants.IOT_HUB_NAME_SUFFIX;

        return await InitializeDeviceAsync(deviceId, iotHubHostName, userCertificate, IsInitializedLoad || checkAuthorization, cancellationToken);
    }

    public async Task ProvisioningAsync(string dpsScopeId, X509Certificate2 certificate, string globalDeviceEndpoint, DeviceMessage.Message message, CancellationToken cancellationToken)
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
            return;
        }

        _logger.Debug($"Registration status: {result.Status}.");
        if (result.Status != ProvisioningRegistrationStatusType.Assigned)
        {
            _logger.Error("Registration status did not assign a hub.");
            return;
        }
        _logger.Info($"Device {result.DeviceId} registered to {result.AssignedHub}.");

        await OnProvisioningCompleted(message, cancellationToken);

        await InitializeDeviceAsync(result.DeviceId, result.AssignedHub, certificate, true, cancellationToken);

    }

    private async Task<bool> InitializeDeviceAsync(string deviceId, string iotHubHostName, X509Certificate2 userCertificate, bool initialize, CancellationToken cancellationToken)
    {
        try
        {
            if (initialize)
            {
                using var auth = _X509CertificateWrapper.GetDeviceAuthentication(deviceId, userCertificate);
                await _deviceClientWrapper.DeviceInitializationAsync(iotHubHostName, auth, cancellationToken);
            }
            return await _deviceClientWrapper.IsDeviceInitializedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception during IoT Hub connection: ", ex);
            return false;
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
            _logger.Error("ProvisioningAsync, Complete message failed", ex);
        }
    }
}