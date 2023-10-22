
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers
{
    public interface IStateMachineHandler
    {
        Task SetState(DeviceStateType state);

        Task InitStateMachineHandler();

        Task<DeviceStateType> GetState();
    }

}