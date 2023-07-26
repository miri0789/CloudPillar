using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinHandler
{
    Task UpdateDeviceStateAsync(DeviceStateType deviceState);
    Task HandleTwinActionsAsync();
    Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReport);
    Task InitReportDeviceParamsAsync();
}