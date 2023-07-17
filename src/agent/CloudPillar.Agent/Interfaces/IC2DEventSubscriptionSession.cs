using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.Interfaces;
public interface IC2DEventSubscriptionSession
{
    Task ReceiveC2DMessagesAsync(DeviceClient deviceClient, CancellationToken cancellationToken);
}
