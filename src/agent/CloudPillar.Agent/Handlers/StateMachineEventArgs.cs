using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public class StateMachineEventArgs : EventArgs
{
    public DeviceStateType NewState {get;}

    public StateMachineEventArgs(DeviceStateType state)
    {
        NewState = state;
    }
}

public delegate void StateMachineEventHandler(object sender, StateMachineEventArgs e);