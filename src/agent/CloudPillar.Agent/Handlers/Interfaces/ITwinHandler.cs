using Microsoft.Azure.Devices.Shared;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinHandler
{
    Task UpdateDeviceStateAsync(DeviceStateType deviceState, CancellationToken cancellationToken);
    Task HandleTwinActionsAsync(CancellationToken cancellationToken);
    Task InitReportDeviceParamsAsync(CancellationToken cancellationToken);
    Task<string> GetTwinJsonAsync(CancellationToken cancellationToken = default);
    Task UpdateDeviceSecretKeyAsync(string secretKey, CancellationToken cancellationToken);
    Task UpdateDeviceCustomPropsAsync(List<TwinReportedCustomProp> customProps, CancellationToken cancellationToken = default);
    Task<DeviceStateType?> GetDeviceStateAsync(CancellationToken cancellationToken = default);
    Task OnDesiredPropertiesUpdateAsync(CancellationToken cancellationToken);
    Task SaveLastTwinAsync(CancellationToken cancellationToken = default);
    Task<string> GetLatestTwinAsync(CancellationToken cancellationToken = default);
}