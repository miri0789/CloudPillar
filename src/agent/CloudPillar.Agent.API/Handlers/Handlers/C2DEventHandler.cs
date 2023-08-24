using CloudPillar.Agent.API.Wrappers;

namespace CloudPillar.Agent.API.Handlers;

public class C2DEventHandler : IC2DEventHandler
{
    private readonly IDeviceClientWrapper _deviceClient;
    private readonly IC2DEventSubscriptionSession _c2DEventSubscriptionSession;
    

    public C2DEventHandler(IDeviceClientWrapper deviceClientWrapper,
     IC2DEventSubscriptionSession c2DEventSubscriptionSession)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(c2DEventSubscriptionSession);

        _deviceClient = deviceClientWrapper;
        _c2DEventSubscriptionSession = c2DEventSubscriptionSession;
    }


    public async Task CreateSubscribeAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Subscribing to C2D messages...");

        await Task.Run(() => _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(cancellationToken));
    }

}