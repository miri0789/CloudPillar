
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers
{
    public interface IStateMachineHandler
    {
        Task SetStateAsync(DeviceStateType state);

        Task InitStateMachineHandlerAsync();

        Task<DeviceStateType> GetStateAsync();
    }

}