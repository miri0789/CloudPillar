
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Shared.Logger;
using TransportType = Microsoft.Azure.Devices.Client.TransportType;
namespace CloudPillar.Agent.Handlers;

public class X509DPSProvisioningDeviceClientHandler : IDPSProvisioningDeviceClientHandler
{
    private const string CLOUD_PILLAR_SUBJECT = "CloudPillar-";
    private const string CERTIFICATE_SUBJECT = "CN=";

    //that the code of iotHubHostName in extention in certificate
    private const string IOT_HUB_HOST_NAME_EXTENTION_KEY = "2.2.2.2";
    private const char CERTIFICATE_NAME_SEPARATOR = '@';
    private const string IOT_HUB_NAME_SUFFIX = ".azure-devices.net";
    private readonly ILoggerHandler _logger;

    private IDeviceClientWrapper _deviceClientWrapper;
    private readonly IX509CertificateWrapper _X509CertificateWrapper;

    public X509DPSProvisioningDeviceClientHandler(
        ILoggerHandler loggerHandler,
        IDeviceClientWrapper deviceClientWrapper,
        IX509CertificateWrapper X509CertificateWrapper)
    {
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _X509CertificateWrapper = X509CertificateWrapper ?? throw new ArgumentNullException(nameof(X509CertificateWrapper));
    }

    public X509Certificate2? GetCertificate()
    {
        using (X509Store store = _X509CertificateWrapper.GetStore(StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certificates = store.Certificates;
            if (certificates == null)
            {
                return null;
            }
            var filteredCertificate = certificates.Cast<X509Certificate2>()
               .Where(cert => cert.Subject.StartsWith(CERTIFICATE_SUBJECT + CLOUD_PILLAR_SUBJECT))
               .FirstOrDefault();

            return filteredCertificate;
        }
    }

    public async Task<bool> AuthorizationAsync(X509Certificate2 userCertificate, CancellationToken cancellationToken)
    {
        if (userCertificate == null)
        {
            _logger.Error($"AuthorizationAsync certificate cant be null");
            return false;
        }

        var friendlyName = userCertificate?.FriendlyName ?? throw new ArgumentNullException(nameof(userCertificate.FriendlyName));
        var parts = friendlyName.Split(CERTIFICATE_NAME_SEPARATOR);

        if (parts.Length != 2)
        {
            var error = "The FriendlyName is not in the expected format.";
            _logger.Error(error);
            throw new ArgumentException(error);
        }

        var deviceId = parts[0];
        var iotHubHostName = parts[1];

        if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(iotHubHostName))
        {
            var error = "The deviceId or the iotHubHostName cant be null.";
            _logger.Error(error);
            throw new ArgumentException(error);
        }

        deviceId = CLOUD_PILLAR_SUBJECT + deviceId;
        iotHubHostName += IOT_HUB_NAME_SUFFIX;
        return await CheckAuthorizationAndInitializeDeviceAsync(deviceId, iotHubHostName, userCertificate, cancellationToken);
    }

    public async Task ProvisioningAsync(string dpsScopeId, X509Certificate2 certificate, string globalDeviceEndpoint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(dpsScopeId);
        ArgumentNullException.ThrowIfNullOrEmpty(globalDeviceEndpoint);
        ArgumentNullException.ThrowIfNull(certificate);

        using var security = _X509CertificateWrapper.GetSecurityProvider(certificate);

        _logger.Debug($"Initializing the device provisioning client...");

        using ProvisioningTransportHandler transport = _deviceClientWrapper.GetProvisioningTransportHandler();
        var provClient = ProvisioningDeviceClient.Create(
            globalDeviceEndpoint,
            dpsScopeId,
            security,
            transport);

        _logger.Debug($"Initialized for registration Id {security.GetRegistrationID()}.");

        DeviceRegistrationResult result = await provClient.RegisterAsync();

        _logger.Debug($"Registration status: {result.Status}.");
        if (result.Status != ProvisioningRegistrationStatusType.Assigned)
        {
            _logger.Error("Registration status did not assign a hub.");
            return;
        }
        _logger.Info($"Device {result.DeviceId} registered to {result.AssignedHub}.");

        await CheckAuthorizationAndInitializeDeviceAsync(result.DeviceId, result.AssignedHub, certificate, cancellationToken);

    }

    private string GetDeviceIdFromCertificate(X509Certificate2 userCertificate)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(userCertificate?.FriendlyName);

        var friendlyName = userCertificate.FriendlyName;
        return friendlyName.Split("@")[0];
    }
    private string GetIotHubHostNameFromCertificate(X509Certificate2 userCertificate)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(userCertificate?.FriendlyName);

        var friendlyName = userCertificate.FriendlyName;
        return friendlyName.Split("@")[1];
    }

    private async Task<bool> CheckAuthorizationAndInitializeDeviceAsync(string deviceId, string iotHubHostName, X509Certificate2 userCertificate, CancellationToken cancellationToken)
    {
        try
        {
            using var auth = _X509CertificateWrapper.GetDeviceAuthentication(deviceId, userCertificate);
            await _deviceClientWrapper.DeviceInitializationAsync(iotHubHostName, auth, cancellationToken);
            return await _deviceClientWrapper.IsDeviceInitializedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception during IoT Hub connection: ", ex);
            return false;
        }
    }    

}