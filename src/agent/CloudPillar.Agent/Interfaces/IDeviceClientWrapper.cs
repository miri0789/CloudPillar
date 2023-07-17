using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.Interfaces;
public interface IDeviceClientWrapper
{
    DeviceClient CreateDeviceClient();
    Task<Message> ReceiveAsync(CancellationToken cancellationToken, DeviceClient deviceClient);

    string GetDeviceId();

    TransportType GetTransportType();

    Task SendEventAsync(Message message, DeviceClient deviceClient);

    Task CompleteAsync(Message message, DeviceClient deviceClient);
}
