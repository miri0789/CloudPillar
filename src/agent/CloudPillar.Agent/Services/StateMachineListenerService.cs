
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Sevices;
public class StateMachineListenerService : BackgroundService
{
    private readonly IStateMachineChangedEvent _stateMachineChangedEvent;
    private readonly IServiceProvider _serviceProvider;
    private readonly IC2DEventHandler _c2DEventHandler;

    //private readonly IStateMachineHandler _stateMachineHandler;
    private IC2DEventSubscriptionSession _c2DEventSubscriptionSession;
    private static CancellationTokenSource _cts;

    public StateMachineListenerService(IStateMachineChangedEvent stateMachineChangedEvent,
    // IC2DEventSubscriptionSession c2DEventSubscriptionSession
    //IC2DEventHandler c2DEventHandler
    IServiceProvider serviceProvider

    )
    {
        _cts = new CancellationTokenSource();
        _stateMachineChangedEvent = stateMachineChangedEvent ?? throw new ArgumentNullException(nameof(stateMachineChangedEvent));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        //this.c2DEventHandler = c2DEventHandler;
        //_stateMachineHandler = stateMachineHandler ?? throw new ArgumentNullException(nameof(stateMachineHandler));
        //_c2DEventSubscriptionSession = c2DEventSubscriptionSession ?? throw new ArgumentNullException(nameof(c2DEventSubscriptionSession));
        // _c2DEventHandler = c2DEventHandler ?? throw new ArgumentException(nameof(c2DEventHandler));

        // _c2DEventHandler = c2DEventHandler ?? throw new ArgumentNullException(nameof(c2DEventHandler));
        // _c2DEventHandlerService = c2DEventHandlerService ?? throw new ArgumentNullException(nameof(c2DEventHandler));

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stateMachineChangedEvent.StateChanged += HandleStateChangedEvent;
        using (var scope = _serviceProvider.CreateScope())
        {
            _c2DEventSubscriptionSession = scope.ServiceProvider.GetService<IC2DEventSubscriptionSession>() ?? throw new ArgumentNullException(nameof(_c2DEventSubscriptionSession));
            var dpsProvisioningDeviceClientHandler = scope.ServiceProvider.GetService<IDPSProvisioningDeviceClientHandler>();
            await dpsProvisioningDeviceClientHandler.InitAuthorizationAsync();

            var StateMachineHandlerService = scope.ServiceProvider.GetService<IStateMachineHandler>();
            StateMachineHandlerService.InitStateMachineHandlerAsync();
        }
       // return Task.CompletedTask;
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