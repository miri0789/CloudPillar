using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent;
public class DeviceClientWrapper: IDeviceClientWrapper
{
    public DeviceClient CreateFromConnectionString(string deviceConnectionString, string transportType)
    {
        return DeviceClient.CreateFromConnectionString(deviceConnectionString, transportType);
    }

    public Task<Message> ReceiveAsync(CancellationToken cancellationToken, DeviceClient deviceClient)
    {
        return deviceClient.ReceiveAsync(cancellationToken);
    }




}
