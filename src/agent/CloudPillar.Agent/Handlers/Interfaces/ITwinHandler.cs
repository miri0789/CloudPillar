using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinHandler
{
    Task GetTwinReportAsync();
    Task UpdateDeviceStateAsync(DeviceStateType deviceState);

}