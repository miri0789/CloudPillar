using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinHandler
{
    Task UpdateDeviceStateAsync(DeviceStateType deviceState);
    Task HandleTwinActionsAsync(CancellationToken cancellationToken);
    Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReport, CancellationToken cancellationToken = default);
    Task InitReportDeviceParamsAsync();
    Task<string> GetTwinJsonAsync(CancellationToken cancellationToken = default);
}