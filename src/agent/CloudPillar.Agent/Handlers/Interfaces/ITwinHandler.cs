using Microsoft.Azure.Devices.Shared;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinHandler
{
    Task UpdateDeviceStateAsync(DeviceStateType deviceState);
    Task HandleTwinActionsAsync(CancellationToken cancellationToken);
    Task InitReportDeviceParamsAsync();
    Task<string> GetTwinJsonAsync(CancellationToken cancellationToken = default);
    Task UpdateDeviceSecretKeyAsync(string secretKey);
    Task UpdateDeviceCustomPropsAsync(List<TwinReportedCustomProp> customProps, CancellationToken cancellationToken = default);
    Task<DeviceStateType?> GetDeviceStateAsync(CancellationToken cancellationToken = default);
    Task OnDesiredPropertiesUpdateAsync(CancellationToken cancellationToken, bool isInitial = false);
    Task SaveLastTwinAsync(CancellationToken cancellationToken = default);
    Task<string> GetLatestTwinAsync(CancellationToken cancellationToken = default);
    Task UpdateReportedTwinChangeSignAsync(string message);
}