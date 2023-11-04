using CloudPillar.Agent.Wrappers;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class C2DEventHandler : IC2DEventHandler
{
    //private readonly IDeviceClientWrapper _deviceClient;
    private readonly IC2DEventSubscriptionSession _c2DEventSubscriptionSession;

    private readonly ILoggerHandler _logger;


    public C2DEventHandler(IDeviceClientWrapper deviceClientWrapper,
     IC2DEventSubscriptionSession c2DEventSubscriptionSession,
     ILoggerHandler logger)
    {
        //_deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _c2DEventSubscriptionSession = c2DEventSubscriptionSession ?? throw new ArgumentNullException(nameof(c2DEventSubscriptionSession));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    


    public async Task CreateSubscribeAsync(CancellationToken cancellationToken, bool isProvisioning)
    {
        _logger.Info("Subscribing to Provisioning C2D messages...");

        await _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(cancellationToken, isProvisioning);
    }

}