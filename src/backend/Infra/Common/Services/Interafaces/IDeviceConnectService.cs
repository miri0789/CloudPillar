using Microsoft.Azure.Devices;

namespace Backend.Infra.Common;

public interface IDeviceConnectService
{
    Task SendDeviceMessageAsync(Message c2dMessage, string deviceId);

    Task SendDeviceMessagesAsync(Message[] c2dMessages, string deviceId);
}