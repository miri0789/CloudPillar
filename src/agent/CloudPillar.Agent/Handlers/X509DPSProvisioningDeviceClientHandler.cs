
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Shared.Logger;
namespace CloudPillar.Agent.Handlers;

public class X509DPSProvisioningDeviceClientHandler : IDPSProvisioningDeviceClientHandler
{
    private readonly ILoggerHandler _logger;

    private IDeviceClientWrapper _deviceClientWrapper;

    public X509DPSProvisioningDeviceClientHandler(ILoggerHandler loggerHandler, IDeviceClientWrapper deviceClientWrapper)
    {
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
    }

    public X509Certificate2 Authenticate()
    {
        using (X509Store store = new X509Store(StoreLocation.CurrentUser))
        {
            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certificates = store.Certificates;
            var filteredCertificate = certificates.Cast<X509Certificate2>()
               .Where(cert => cert.Subject.StartsWith("CN=Bracha"))
               .FirstOrDefault();

            return filteredCertificate;
        }
    }

    public bool Authorization(X509Certificate2 userCertificate)
    {
        try
        {
            using var auth = new DeviceAuthenticationWithX509Certificate("BrachaD", userCertificate);
            using var iotClient = DeviceClient.Create("<your IoT Hub connection string>", auth, TransportType.Amqp);
            if (iotClient != null)
            {
                _deviceClientWrapper.DeviceInitialization(iotClient);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"exception during IoT Hub connection: {ex.Message}");
            return false;
        }
    }

    public async Task ProvisioningAsync(string dpsScopeId, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(dpsScopeId);
        ArgumentNullException.ThrowIfNull(certificate);


        using var security = new SecurityProviderX509Certificate(certificate);

        _logger.Debug($"Initializing the device provisioning client...");

        using ProvisioningTransportHandler transport = GetTransportHandler();
        var provClient = ProvisioningDeviceClient.Create(
            "global.azure-devices-provisioning.net",
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
        

    }

    // public async Task Provisioning(string dpsScopeId, string certificateThumbprint)
    // {
    //     ArgumentNullException.ThrowIfNull(dpsScopeId);
    //     ArgumentNullException.ThrowIfNull(certificateThumbprint);
    //     //string dpsScopeId = "0ne00AF7B07";

    //     // Specify the thumbprint of the certificate you want to use from the certificate store
    //     // string certificateThumbprint = "0579697e370b29c7ee5a29da0cadfa005ad0a19b";

    //     // Find the certificate in the certificate store by thumbprint
    //     using (X509Store store = new X509Store(StoreLocation.CurrentUser))
    //     {
    //         store.Open(OpenFlags.ReadOnly);
    //         X509Certificate2Collection certificates = store.Certificates.Find(
    //             X509FindType.FindByThumbprint, certificateThumbprint, true);

    //         if (certificates.Count == 0)
    //         {
    //             _logger.Error("Certificate not found in the certificate store.");
    //             return;
    //         }

    //         X509Certificate2 deviceCert = certificates[0];

    //         using var security = new SecurityProviderX509Certificate(deviceCert);

    //         _logger.Debug($"Initializing the device provisioning client...");

    //         using ProvisioningTransportHandler transport = GetTransportHandler();
    //         var provClient = ProvisioningDeviceClient.Create(
    //             "global.azure-devices-provisioning.net",
    //             dpsScopeId,
    //             security,
    //             transport);

    //         _logger.Debug($"Initialized for registration Id {security.GetRegistrationID()}.");

    //         _logger.Debug("Registering with the device provisioning service... ");
    //         DeviceRegistrationResult result = await provClient.RegisterAsync();

    //         _logger.Debug($"Registration status: {result.Status}.");
    //         if (result.Status != ProvisioningRegistrationStatusType.Assigned)
    //         {
    //             _logger.Error($"Registration status did not assign a hub.");
    //             return;
    //         }

    //         _logger.Debug($"Device {result.DeviceId} registered to {result.AssignedHub}.");

    //         // _logger.Debug("Creating X509 authentication for IoT Hub...");
    //         // using var auth = new DeviceAuthenticationWithX509Certificate(
    //         //     result.DeviceId,
    //         //     deviceCert);



    //         // _logger.Debug($"Testing the provisioned device with IoT Hub...");
    //         // using var iotClient = DeviceClient.Create(result.AssignedHub, auth, TransportType.Amqp);

    //         // _logger.Debug("Sending a telemetry message...");
    //         // using var message = new Message(Encoding.UTF8.GetBytes("TestMessage"));
    //         // await iotClient.SendEventAsync(message);

    //         // await iotClient.CloseAsync();

    //         store.Close();
    //     }

    // }

    private ProvisioningTransportHandler GetTransportHandler()
    {
        return new ProvisioningTransportHandlerAmqp();
        //return new Microsoft.Azure.Devices.Provisioning.Client.Transport.ProvisioningTransportHandlerMqtt();
        // return TransportType.Amqp
        // {
        //     TransportType.Mqtt => new ProvisioningTransportHandlerMqtt(),
        //     TransportType.Mqtt_Tcp_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly),
        //     TransportType.Mqtt_WebSocket_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly),
        //     TransportType.Amqp => new ProvisioningTransportHandlerAmqp(),
        //     TransportType.Amqp_Tcp_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly),
        //     TransportType.Amqp_WebSocket_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.WebSocketOnly),
        //     TransportType.Http1 => new ProvisioningTransportHandlerHttp(),
        //     _ => throw new NotSupportedException($"Unsupported transport type {_parameters.TransportType}"),
        // };
    }


    // public Task<bool> Authentication()
    // {
    //     using (X509Store store = new X509Store(StoreLocation.CurrentUser))
    //     {
    //         store.Open(OpenFlags.ReadOnly);
    //         X509Certificate2Collection certificates = store.Certificates;
    //         var filteredCertificate = certificates.Cast<X509Certificate2>()
    //        .Where(cert => cert.Subject.StartsWith("CN=Bracha"))
    //        .FirstOrDefault();

    //         if (filteredCertificate != null)
    //         {
    //             _logger.Debug("Creating X509 authentication for IoT Hub...");
    //             using var auth = new DeviceAuthenticationWithX509Certificate(
    //                 "BrachaD",
    //                 filteredCertificate);


    //             _logger.Debug($"Testing the provisioned device with IoT Hub...");
    //             using var iotClient = DeviceClient.Create("", auth, TransportType.Amqp);

    //             if(iotClient != null)
    //             {

    //             }

    //             // _logger.Debug("Sending a telemetry message...");
    //             // using var message = new Message(Encoding.UTF8.GetBytes("TestMessage"));
    //             // await iotClient.SendEventAsync(message);

    //             // await iotClient.CloseAsync();
    //         }


    //         // X509Certificate2Collection certificates = store.Certificates.Find(
    //         //     X509FindType.FindBySubjectName, certificateThumbprint, false);

    //         // if (certificates.Count == 0)
    //         // {
    //         //     _logger.Error("Certificate not found in the certificate store.");
    //         //     return false;
    //         // }
    //         store.Close();
    //     }
    // }
}