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
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _c2DEventSubscriptionSession = c2DEventSubscriptionSession ?? throw new ArgumentNullException(nameof(c2DEventSubscriptionSession));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }


    public void CreateSubscribe(CancellationToken cancellationToken)
    {
        _logger.Info("Subscribing to C2D messages...");

        Task.Run(() => _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(cancellationToken, false));
    }


    public async Task<bool> CreateProvisioningSubscribe(CancellationToken cancellationToken)
    {
        _logger.Info("Subscribing to Provisioning C2D messages...");

        return await _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(cancellationToken, true);
    }

}