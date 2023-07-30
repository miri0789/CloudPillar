using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public interface IDeviceClientWrapper
{

    string GetDeviceId();

    TransportType GetTransportType();

    Task SendEventAsync(Message message);
    
    Task<Message> ReceiveAsync(CancellationToken cancellationToken);

    Task CompleteAsync(Message message);

    Task<Twin> GetTwinAsync();

    Task UpdateReportedPropertiesAsync(string key, object value);
}
