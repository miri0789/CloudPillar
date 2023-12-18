using Microsoft.Azure.Devices;

namespace Backend.Infra.Common.Services.Interfaces;

public interface IDeviceConnectService
{
    Task SendDeviceMessageAsync(Message c2dMessage, string deviceId);

    Task SendDeviceMessageAsync(ServiceClient serviceClient, Message c2dMessage, string deviceId);

    Task SendDeviceMessagesAsync(Message[] c2dMessages, string deviceId);
}