using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.Handlers;
public interface IC2DEventSubscriptionSession
{
    Task<bool> ReceiveC2DMessagesAsync(CancellationToken cancellationToken, bool isProvisioning);
}
