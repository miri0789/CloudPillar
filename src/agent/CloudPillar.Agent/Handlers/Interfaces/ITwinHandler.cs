using Microsoft.Azure.Devices.Shared;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinHandler
{
    Task UpdateDeviceStateAsync(DeviceStateType deviceState);
    Task HandleTwinActionsAsync(CancellationToken cancellationToken);
    Task InitReportDeviceParamsAsync();
    Task<Twin> GetTwinJsonAsync(CancellationToken cancellationToken = default);

    Task<DeviceStateType?> GetDeviceStateAsync(CancellationToken cancellationToken = default);
}