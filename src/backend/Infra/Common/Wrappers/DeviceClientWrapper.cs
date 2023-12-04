using Backend.Infra.Common.Wrappers.Interfaces;
using Microsoft.Azure.Devices;
using Shared.Logger;

namespace Backend.Infra.Wrappers;
public class DeviceClientWrapper : IDeviceClientWrapper
{
    private readonly ILoggerHandler _logger;

    public DeviceClientWrapper(ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ServiceClient CreateFromConnectionString(string connString)
    {
        var serviceClient = ServiceClient.CreateFromConnectionString(connString);
        return serviceClient;
    }



    public async Task SendAsync(ServiceClient serviceClient, string deviceId, Message c2dMessage)
    {
        await serviceClient.SendAsync(deviceId, c2dMessage);
    }
}