using CloudPillar.Agent.API.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.API.Handlers;

public interface ITwinHandler
{
    Task UpdateDeviceStateAsync(DeviceStateType deviceState);
    Task HandleTwinActionsAsync();
    Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReport);
    Task InitReportDeviceParamsAsync();
}