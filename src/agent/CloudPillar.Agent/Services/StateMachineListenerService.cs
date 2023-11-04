
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Sevices;
public class StateMachineListenerService : BackgroundService
{
    private readonly IStateMachineChangedEvent _stateMachineChangedEvent;
    private readonly IC2DEventHandler _c2DEventHandler;

    //private readonly IStateMachineHandler _stateMachineHandler;
    //private readonly IC2DEventSubscriptionSession _c2DEventSubscriptionSession;
    private static CancellationTokenSource _cts;

    public StateMachineListenerService(IStateMachineChangedEvent stateMachineChangedEvent,
    // IC2DEventSubscriptionSession c2DEventSubscriptionSession
    IC2DEventHandler c2DEventHandler

    )
    {
        _cts = new CancellationTokenSource();
        _stateMachineChangedEvent = stateMachineChangedEvent ?? throw new ArgumentNullException(nameof(stateMachineChangedEvent));
        //this.c2DEventHandler = c2DEventHandler;
        //_stateMachineHandler = stateMachineHandler ?? throw new ArgumentNullException(nameof(stateMachineHandler));
        //_c2DEventSubscriptionSession = c2DEventSubscriptionSession ?? throw new ArgumentNullException(nameof(c2DEventSubscriptionSession));
         _c2DEventHandler = c2DEventHandler ?? throw new ArgumentException(nameof(c2DEventHandler));

        // _c2DEventHandler = c2DEventHandler ?? throw new ArgumentNullException(nameof(c2DEventHandler));
        // _c2DEventHandlerService = c2DEventHandlerService ?? throw new ArgumentNullException(nameof(c2DEventHandler));

    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stateMachineChangedEvent.StateChanged += HandleStateChangedEvent;
        return Task.CompletedTask;
    }

    private async void HandleStateChangedEvent(object? sender, StateMachineEventArgs e)
    {
        switch (e.NewState)
        {
            case DeviceStateType.Provisioning:
                await SetProvisioningAsync();
                break;
            case DeviceStateType.Ready:
                await SetReadyAsync();
                break;
            case DeviceStateType.Busy:
                SetBusy();
                break;
            default:
                break;
        }
    }
    private async Task SetProvisioningAsync()
    {
        _cts = new CancellationTokenSource();
        await _c2DEventHandler.CreateSubscribeAsync(_cts.Token, true);
    }

    private async Task SetReadyAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        await _c2DEventHandler.CreateSubscribeAsync(_cts.Token, false);
        //await _twinHandler.HandleTwinActionsAsync(_cts.Token);
    }

    private void SetBusy()
    {
        _cts?.Cancel();
    }


}