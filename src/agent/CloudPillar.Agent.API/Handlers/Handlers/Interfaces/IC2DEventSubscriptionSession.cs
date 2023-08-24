using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.API.Handlers;
public interface IC2DEventSubscriptionSession
{
    Task ReceiveC2DMessagesAsync(CancellationToken cancellationToken);
}
