
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers
{
    public interface IStateMachine
    {
        Task SetState(DeviceStateType state);
    }

}