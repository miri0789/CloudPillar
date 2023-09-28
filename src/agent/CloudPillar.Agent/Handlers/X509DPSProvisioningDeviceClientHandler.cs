
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
    private const string CLOUD_PILLAR_SUBJECT = "CN=CloudPillar-";
    private const string CERTIFICATE_SUBJECT = "CN=";

    //that the code of iotHubHostName in extention in certificate
    private const string IOT_HUB_HOST_NAME_EXTENTION_KEY = "2.2.2.2";
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
               .Where(cert => cert.Subject.StartsWith(CLOUD_PILLAR_SUBJECT))
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

        var deviceId = GetDeviceIdFromCertificate(userCertificate);
        var iotHubHostName = GetIotHubHostNameFromCertificate(userCertificate);

        if (string.IsNullOrEmpty(iotHubHostName))
        {
            _logger.Error($"AuthorizationAsync certificate must have iotHubHostName extention");
            return false;
        }

        return await CheckAuthorizationAndInitializeDeviceAsync(deviceId, iotHubHostName, userCertificate, cancellationToken);
    }

    public async Task ProvisioningAsync(string dpsScopeId, X509Certificate2 certificate, string globalDeviceEndpoint, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(dpsScopeId);
        ArgumentNullException.ThrowIfNullOrEmpty(globalDeviceEndpoint);
        ArgumentNullException.ThrowIfNull(certificate);

        using var security = _X509CertificateWrapper.GetSecurityProvider(certificate);

        _logger.Debug($"Initializing the device provisioning client...");

        using ProvisioningTransportHandler transport = GetTransportHandler();
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

    private string GetIotHubHostNameFromCertificate(X509Certificate2 userCertificate)
    {
        var iotHubHostName = string.Empty;
        foreach (X509Extension extension in userCertificate.Extensions)
        {

            if (extension.Oid?.Value == IOT_HUB_HOST_NAME_EXTENTION_KEY)
            {
                iotHubHostName = Encoding.UTF8.GetString(extension.RawData);
            }
        }
        return iotHubHostName;
    }

    private async Task<bool> IsDeviceInitializedAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if the device is already initialized
            await _deviceClientWrapper.GetTwinAsync(cancellationToken);
            return true;
        }
        catch
        {
            _logger.Debug($"IsDeviceInitializedAsync, Device is not initialized.");
            return false;
        }
    }

    private string GetDeviceIdFromCertificate(X509Certificate2 userCertificate)
    {
        if (string.IsNullOrEmpty(userCertificate.Subject))
        {
            throw new ArgumentNullException(nameof(userCertificate.Subject));
        }
        return userCertificate.Subject.Replace(CERTIFICATE_SUBJECT, string.Empty);
    }

    private async Task<bool> CheckAuthorizationAndInitializeDeviceAsync(string deviceId, string iotHubHostName, X509Certificate2 userCertificate, CancellationToken cancellationToken)
    {
        try
        {
            using var auth = _X509CertificateWrapper.GetDeviceAuthentication(deviceId, userCertificate);
            await _deviceClientWrapper.DeviceInitializationAsync(iotHubHostName, auth, cancellationToken);
            return await IsDeviceInitializedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception during IoT Hub connection: ", ex);
            return false;
        }
    }

    private ProvisioningTransportHandler GetTransportHandler()
    {
        return _deviceClientWrapper.GetTransportType() switch
        {
            TransportType.Mqtt => new ProvisioningTransportHandlerMqtt(),
            TransportType.Mqtt_Tcp_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly),
            TransportType.Mqtt_WebSocket_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly),
            TransportType.Amqp => new ProvisioningTransportHandlerAmqp(),
            TransportType.Amqp_Tcp_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly),
            TransportType.Amqp_WebSocket_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.WebSocketOnly),
            TransportType.Http1 => new ProvisioningTransportHandlerHttp(),
            _ => throw new NotSupportedException($"Unsupported transport type {_deviceClientWrapper.GetTransportType()}"),
        };
    }

}