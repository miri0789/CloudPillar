
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
    private readonly ILoggerHandler _logger;

    private IDeviceClientWrapper _deviceClientWrapper;

    public X509DPSProvisioningDeviceClientHandler(ILoggerHandler loggerHandler, IDeviceClientWrapper deviceClientWrapper)
    {
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
    }

    public X509Certificate2 GetCertificate()
    {
        using (X509Store store = new X509Store(StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certificates = store.Certificates;
            var filteredCertificate = certificates.Cast<X509Certificate2>()
               .Where(cert => cert.Subject.StartsWith(CLOUD_PILLAR_SUBJECT))
               .FirstOrDefault();

            return filteredCertificate;
        }
    }

    public async Task<bool> AuthorizationAsync(X509Certificate2 userCertificate)
    {
        ArgumentNullException.ThrowIfNull(userCertificate);
        var deviceId = userCertificate.Subject.Replace(CLOUD_PILLAR_SUBJECT, string.Empty);
        var iotHubHostNameExtention = userCertificate.Extensions.FirstOrDefault(ext => ext.Oid.FriendlyName == "iotHubHostName");
        ArgumentNullException.ThrowIfNull(iotHubHostNameExtention);
        var iotHubHostName = Encoding.UTF8.GetString(iotHubHostNameExtention.RawData);
        try
        {

            using var auth = new DeviceAuthenticationWithX509Certificate(deviceId, userCertificate);
            var iotClient = DeviceClient.Create(iotHubHostName, auth, _deviceClientWrapper.GetTransportType());
            if (iotClient != null)
            {
                // iotClient never return null also if device not exist, so to check if device is exist, or the certificate is valid we try to get the device twin.
                var twin = await iotClient.GetTwinAsync();
                if (twin != null)
                {
                    _deviceClientWrapper.DeviceInitialization(iotClient);
                    return true;
                }
            }

            _logger.Info($"Device {deviceId}, is not exist in {iotHubHostName}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Exception during IoT Hub connection: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ProvisioningAsync(string dpsScopeId, X509Certificate2 certificate, string globalDeviceEndpoint = "global.azure-devices-provisioning.net")
    {
        ArgumentNullException.ThrowIfNull(dpsScopeId);
        ArgumentNullException.ThrowIfNull(certificate);

        try
        {
            using var security = new SecurityProviderX509Certificate(certificate);

            _logger.Debug($"Initializing the device provisioning client...");

            using ProvisioningTransportHandler transport = GetTransportHandler();
            var provClient = ProvisioningDeviceClient.Create(
                globalDeviceEndpoint,
                dpsScopeId,
                security,
                transport);

            _logger.Debug($"Initialized for registration Id {security.GetRegistrationID()}.");

            _logger.Debug("Registering with the device provisioning service... ");
            DeviceRegistrationResult result = await provClient.RegisterAsync();

            _logger.Debug($"Registration status: {result.Status}.");
            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                throw new Exception("Registration status did not assign a hub.");
            }
            _logger.Debug($"Device {result.DeviceId} registered to {result.AssignedHub}.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed on Provisioning - {ex.Message}");
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