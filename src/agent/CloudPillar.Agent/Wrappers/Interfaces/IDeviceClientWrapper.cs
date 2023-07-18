using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.Wrappers;
public interface IDeviceClientWrapper
{
    DeviceClient CreateDeviceClient();
    Task<Message> ReceiveAsync(CancellationToken cancellationToken, DeviceClient deviceClient);

    string GetDeviceId();

    TransportType GetTransportType();

    Task SendEventAsync(Message message, DeviceClient deviceClient);
}
