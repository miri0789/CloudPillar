using Microsoft.Azure.Devices;

namespace Backend.Infra.Common;

public interface IDeviceConnectService
{
    ServiceClient CreateFromConnectionString(string connString);
    Task SendMessage(ServiceClient serviceClient, Message c2dMessage, string deviceId);
}