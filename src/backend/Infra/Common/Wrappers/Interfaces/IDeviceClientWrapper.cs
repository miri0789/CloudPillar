using Microsoft.Azure.Devices;

namespace Backend.Infra.Common.Wrappers.Interfaces;
public interface IDeviceClientWrapper
{
    ServiceClient CreateFromConnectionString(string connString);
    Task SendAsync(ServiceClient serviceClient, string deviceId, Message c2dMessage);
}