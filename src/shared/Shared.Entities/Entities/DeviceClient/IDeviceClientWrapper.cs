using Microsoft.Azure.Devices;

namespace Shared.Entities.DeviceClient;
public interface IDeviceClientWrapper
{
    ServiceClient CreateFromConnectionString(string connString);
    Task SendAsync(ServiceClient _serviceClient, string deviceId, Message c2dMessage);
}