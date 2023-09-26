using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinHandler
{
    Task UpdateDeviceStateAsync(DeviceStateType deviceState);
    Task HandleTwinActionsAsync(CancellationToken cancellationToken);
    Task InitReportDeviceParamsAsync();
    Task<string> GetTwinJsonAsync();
}