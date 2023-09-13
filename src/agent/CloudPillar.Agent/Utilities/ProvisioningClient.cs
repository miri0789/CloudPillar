using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Utilities;
public class ProvisioningClient
{

    public async void RegisterCertificate()
    {
        
        string dpsScopeId = "0ne00AF7B07";
        
        // Specify the thumbprint of the certificate you want to use from the certificate store
        string certificateThumbprint = "0579697e370b29c7ee5a29da0cadfa005ad0a19b";

        // Find the certificate in the certificate store by thumbprint
        X509Store store = new X509Store(StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadOnly);
        X509Certificate2Collection certificates = store.Certificates.Find(
            X509FindType.FindByThumbprint, certificateThumbprint, false);

        if (certificates.Count == 0)
        {
            Console.WriteLine("Certificate not found in the certificate store.");
            return;
        }

        X509Certificate2 deviceCert = certificates[0];

        using var security = new SecurityProviderX509Certificate(deviceCert);

        Console.WriteLine($"Initializing the device provisioning client...");

        using ProvisioningTransportHandler transport = GetTransportHandler();
        var provClient = ProvisioningDeviceClient.Create(
            "global.azure-devices-provisioning.net",
            dpsScopeId,
            security,
            transport);

        Console.WriteLine($"Initialized for registration Id {security.GetRegistrationID()}.");

        Console.WriteLine("Registering with the device provisioning service... ");
        DeviceRegistrationResult result = await provClient.RegisterAsync();

        Console.WriteLine($"Registration status: {result.Status}.");
        if (result.Status != ProvisioningRegistrationStatusType.Assigned)
        {
            Console.WriteLine($"Registration status did not assign a hub, so exiting this sample.");
            return;
        }

        Console.WriteLine($"Device {result.DeviceId} registered to {result.AssignedHub}.");

        Console.WriteLine("Creating X509 authentication for IoT Hub...");
        using var auth = new DeviceAuthenticationWithX509Certificate(
            result.DeviceId,
            deviceCert);

        Console.WriteLine($"Testing the provisioned device with IoT Hub...");
        using var iotClient = DeviceClient.Create(result.AssignedHub, auth, TransportType.Amqp);

        Console.WriteLine("Sending a telemetry message...");
        using var message = new Message(Encoding.UTF8.GetBytes("TestMessage"));
        await iotClient.SendEventAsync(message);

        await iotClient.CloseAsync();
        Console.WriteLine("Finished.");

        // var security = new SecurityProviderX509Certificate(deviceCert);
        // var transport = new ProvisioningTransportHandlerHttp();

        // using var provisioningClient = ProvisioningDeviceClient.Create(
        //     "global.azure-devices-provisioning.net", dpsScopeId, security, transport);

        // Console.WriteLine("Provisioning device...");
        // DeviceRegistrationResult result = await provisioningClient.RegisterAsync();

        // if (result.Status == ProvisioningRegistrationStatusType.Assigned)
        // {
        //     Console.WriteLine($"DeviceID: {result.DeviceId}");

        //     // Create a device client
        //     using var deviceClient = DeviceClient.Create(
        //         result.AssignedHub,
        //         AuthenticationMethodFactory.CreateAuthenticationWithCertificate(result.DeviceId, deviceCert),
        //         TransportType.Mqtt);

        //     // Your device-specific logic here
        //     // For example, sending telemetry data or receiving commands

        //     // Close the device client and clean up resources
        //     await deviceClient.CloseAsync();
        // }
        // else
        // {
        //     Console.WriteLine($"Device registration failed: {result.Status}");
        // }

        // Close the certificate store
        store.Close();
       
    }

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

}








