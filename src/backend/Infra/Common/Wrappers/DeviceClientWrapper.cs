using Backend.Infra.Common.Wrappers.Interfaces;
using Microsoft.Azure.Devices;

namespace Backend.Infra.Wrappers;
public class DeviceClientWrapper : IDeviceClientWrapper
{   
    

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