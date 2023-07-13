using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent;
public interface IDeviceClientWrapper
{
    DeviceClient CreateFromConnectionString(string deviceConnectionString, string transportType);
    Task<Message> ReceiveAsync(CancellationToken cancellationToken, DeviceClient deviceClient);

}
