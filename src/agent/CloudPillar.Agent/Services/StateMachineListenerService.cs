
using CloudPillar.Agent.Handlers;

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

        // throw new NotImplementedException();
    }
}