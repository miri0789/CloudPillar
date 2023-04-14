using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;

namespace AzureIoTHubExample
{
    class Program
    {
        private static readonly string connectionString = "HostName=szlabs-iot-hub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=dMBNypodzUSWPbxTXdWaV4PxJTR3jCwehPFCQn+XJXc=";
        private static readonly string deviceId = "amanaged01";

        static async Task Test(string[] args)
        {
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
            await SendCloudToDeviceMessageAsync(serviceClient, deviceId);
        }

        private static async Task SendCloudToDeviceMessageAsync(ServiceClient serviceClient, string deviceId)
        {
            Console.WriteLine($"Sending C2D message to device {deviceId}...");

            var message = new Message(System.Text.Encoding.ASCII.GetBytes("Hello from .NET Core!"));
            await serviceClient.SendAsync(deviceId, message);

            Console.WriteLine("C2D message sent.");
        }
    }
}
