using CloudPillar.Agent.Wrappers;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class C2DEventHandler : IC2DEventHandler
{
    private readonly IDeviceClientWrapper _deviceClient;
    private readonly IC2DEventSubscriptionSession _c2DEventSubscriptionSession;

    private readonly ILoggerHandler _logger;
    

    public C2DEventHandler(IDeviceClientWrapper deviceClientWrapper,
     IC2DEventSubscriptionSession c2DEventSubscriptionSession,
     ILoggerHandler logger)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(c2DEventSubscriptionSession);

        _deviceClient = deviceClientWrapper;
        _c2DEventSubscriptionSession = c2DEventSubscriptionSession;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public async Task CreateSubscribeAsync(CancellationToken cancellationToken)
    {
        _logger.Info("Subscribing to C2D messages...");

        await Task.Run(() => _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(cancellationToken));
    }

}