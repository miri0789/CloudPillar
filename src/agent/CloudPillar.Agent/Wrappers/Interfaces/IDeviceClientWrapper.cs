using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.Wrappers;
public interface IDeviceClientWrapper
{
    DeviceClient CreateDeviceClient(string connectionString);
    Task<Message> ReceiveAsync(CancellationToken cancellationToken, DeviceClient deviceClient);

    string GetDeviceId(string connectionString);

    TransportType GetTransportType();

    Task SendEventAsync(Message message, DeviceClient deviceClient);

    Task CompleteAsync(Message message, DeviceClient deviceClient);
}
