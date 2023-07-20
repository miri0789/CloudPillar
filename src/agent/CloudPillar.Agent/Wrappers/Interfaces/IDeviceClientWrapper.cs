using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.Wrappers;
public interface IDeviceClientWrapper
{
    Task<Message> ReceiveAsync(CancellationToken cancellationToken);

    string GetDeviceId();

    TransportType GetTransportType();

    Task SendEventAsync(Message message);

    Task CompleteAsync(Message message);
}
