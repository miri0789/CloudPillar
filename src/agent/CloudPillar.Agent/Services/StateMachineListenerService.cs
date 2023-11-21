
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Sevices;
public class StateMachineListenerService : BackgroundService
{
    private readonly IStateMachineChangedEvent _stateMachineChangedEvent;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private CancellationTokenSource _cts;
    private ITwinHandler _twinHandler;
    private IC2DEventSubscriptionSession _c2DEventSubscriptionSession;

    public StateMachineListenerService(
        IStateMachineChangedEvent stateMachineChangedEvent,
    IServiceProvider serviceProvider,
    IDeviceClientWrapper deviceClientWrapper
    )
    {
        _cts = new CancellationTokenSource();
        _stateMachineChangedEvent = stateMachineChangedEvent ?? throw new ArgumentNullException(nameof(stateMachineChangedEvent));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stateMachineChangedEvent.StateChanged += HandleStateChangedEvent;
        using (var scope = _serviceProvider.CreateScope())
        {
            var dpsProvisioningDeviceClientHandler = scope.ServiceProvider.GetService<IDPSProvisioningDeviceClientHandler>();
            ArgumentNullException.ThrowIfNull(dpsProvisioningDeviceClientHandler);
            await dpsProvisioningDeviceClientHandler.InitAuthorizationAsync();

            var StateMachineHandlerService = scope.ServiceProvider.GetService<IStateMachineHandler>();
            ArgumentNullException.ThrowIfNull(StateMachineHandlerService);
            await StateMachineHandlerService.InitStateMachineHandlerAsync();
        }
    }

    internal async void HandleStateChangedEvent(object? sender, StateMachineEventArgs e)
    {
        if (_c2DEventSubscriptionSession == null)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                _c2DEventSubscriptionSession = scope.ServiceProvider.GetService<IC2DEventSubscriptionSession>();
                ArgumentNullException.ThrowIfNull(_c2DEventSubscriptionSession);
                _twinHandler = scope.ServiceProvider.GetService<ITwinHandler>();
                ArgumentNullException.ThrowIfNull(_twinHandler);
            }
        }

        switch (e.NewState)
        {
            case DeviceStateType.Provisioning:
                await SetProvisioningAsync();
                break;
            case DeviceStateType.Ready:
                await SetReadyAsync();
                break;
            case DeviceStateType.Busy:
                await SetBusyAsync();
                break;
            default:
                break;
        }
    }

    private async Task SetProvisioningAsync()
    {
        _cts = new CancellationTokenSource();
        await _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(_cts.Token, true);
    }

    private async Task SetReadyAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var subscribeTask = _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(_cts.Token, false);
        var handleTwinTask = _twinHandler.HandleTwinActionsAsync(_cts.Token);
        await Task.WhenAll(subscribeTask, handleTwinTask);
    }


    private async Task SetBusyAsync()
    {
        await _twinHandler.SaveLastTwinAsync();
        await _deviceClientWrapper.DisposeAsync();
        _cts?.Cancel();
    }


}