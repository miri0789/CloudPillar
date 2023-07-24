using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;
using Polly;
using Shared.Entities.Messages;
using Backend.BlobStreamer.Interfaces;

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
        await _serviceClient.SendAsync(deviceId, c2dMessage);
    }
}