using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinHandler
{
    Task GetTwinReport();
    Task UpdateDeviceState(DeviceStateType deviceState);

}