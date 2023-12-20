using Backend.Infra.Common.Wrappers.Interfaces;
using Microsoft.Azure.Devices;

namespace Backend.Infra.Wrappers;
public class DeviceClientWrapper : IDeviceClientWrapper
{
    private readonly ICommonEnvironmentsWrapper _environmentsWrapper;
    public DeviceClientWrapper(ICommonEnvironmentsWrapper environmentsWrapper)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        ArgumentNullException.ThrowIfNullOrEmpty(_environmentsWrapper.iothubConnectionString);
    }

    public ServiceClient CreateFromConnectionString()
    {
        var serviceClient = ServiceClient.CreateFromConnectionString(_environmentsWrapper.iothubConnectionString);
        return serviceClient;
    }



    public async Task SendAsync(ServiceClient serviceClient, string deviceId, Message c2dMessage)
    {
        await serviceClient.SendAsync(deviceId, c2dMessage);
    }
}