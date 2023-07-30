using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.Wrappers;
public interface IDeviceClientWrapper
{

    string GetDeviceId();

    TransportType GetTransportType();

    Task SendEventAsync(Message message);
    
    Task<Message> ReceiveAsync(CancellationToken cancellationToken);

    Task CompleteAsync(Message message);
}
