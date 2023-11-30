using Microsoft.Azure.Devices;

namespace Backend.Infra.Common;

public interface IDeviceConnectService
{
    Task SendDeviceMessage(Message c2dMessage, string deviceId);

    Task SendDeviceMessages(Message[] c2dMessages, string deviceId);
}