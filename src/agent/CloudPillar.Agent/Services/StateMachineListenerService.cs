
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Sevices;
public class StateMachineListenerService
{
    private readonly IStateMachineHandler _stateMachineHandler;
    private readonly IC2DEventHandler _c2DEventHandler;
    private static CancellationTokenSource _cts;

    public StateMachineListenerService(IStateMachineHandler stateMachineHandler,
    IC2DEventHandler c2DEventHandler)
    {
        _stateMachineHandler = stateMachineHandler ?? throw new ArgumentNullException(nameof(stateMachineHandler));
        _c2DEventHandler = c2DEventHandler ?? throw new ArgumentNullException(nameof(c2DEventHandler));
        _stateMachineHandler.StateChanged += HandleStateChangedEvent;
    }

    private void HandleStateChangedEvent(object? sender, StateMachineEventArgs e)
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
        // throw new NotImplementedException();
    }
    private async Task SetProvisioningAsync()
    {
        _cts.Start();
        await _c2DEventHandler.CreateSubscribeAsync(_cts.Token, true);
    }

    private async Task SetReadyAsync()
    {
        _cts.CancelToken();
        var _cts = _stateMachineTokenHandler.StartToken();
        await _c2DEventHandler.CreateSubscribeAsync(_cts.Token, false);
        await _twinHandler.HandleTwinActionsAsync(_cts.Token);
    }

    private void SetBusy()
    {
         _cts.CancelToken();
        //_stateMachineTokenHandler.CancelToken();
    }
}