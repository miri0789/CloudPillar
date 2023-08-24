using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;
using Polly;
using Shared.Entities.Messages;
using Backend.BlobStreamer.Interfaces;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Backend.BlobStreamer.Services;


public class DeviceClientWrapper : IDeviceClientWrapper
{
    public ServiceClient CreateFromConnectionString(string connString)
    {
        var serviceClient = ServiceClient.CreateFromConnectionString(connString);
        return serviceClient;
    }
    public async Task SendAsync(ServiceClient _serviceClient, string deviceId, Message c2dMessage)
    {
        while (true)
        {
            try
            {
                await _serviceClient.SendAsync(deviceId, c2dMessage);
                break; // Succeeded
            }
            catch (Exception ex)
            {
                const string queueLimitMsg = "C2D messages enqueued for the device exceeded the queue limit";
                const string throttlingBacklogMsg = "Throttling backlog element expired";
                if (ex.Message.Contains(queueLimitMsg) || ex.Message.Contains(throttlingBacklogMsg))
                {
                    Console.WriteLine($"Overflow of 50 messages in the C2D queue, stalling until client '{deviceId}' unloads some.");
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
                else
                {
                    throw ex;
                }
            }
        }
    }
}