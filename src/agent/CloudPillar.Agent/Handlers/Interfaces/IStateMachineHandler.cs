
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers
{
    public interface IStateMachineHandler
    {
        Task SetStateAsync(DeviceStateType state, CancellationToken cancellationToken);

        Task InitStateMachineHandlerAsync();

        Task<DeviceStateType> GetStateAsync();
        
        DeviceStateType GetCurrentDeviceState();
    }

}