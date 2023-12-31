using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinReportHandler
{
    TwinActionReported GetActionToReport(ActionToReport actionToReport, string periodicFileName = "");

    Task UpdateReportedChangeSpecAsync(TwinReportedChangeSpec changeSpec, TwinPatchChangeSpec changeSpecKey, CancellationToken cancellationToken);

    Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReported, CancellationToken cancellationToken);

    Task UpdateDeviceStateAsync(DeviceStateType deviceState, CancellationToken cancellationToken);
    Task<DeviceStateType?> GetDeviceStateAsync(CancellationToken cancellationToken = default);

    Task UpdateDeviceStateAfterServiceRestartAsync(DeviceStateType? deviceState, CancellationToken cancellationToken);
    Task<DeviceStateType?> GetDeviceStateAfterServiceRestartAsync(CancellationToken cancellationToken = default);

    Task UpdateDeviceSecretKeyAsync(string secretKey, CancellationToken cancellationToken);

    Task UpdateDeviceCertificateValidity(int CertificateExpiredDays, CancellationToken cancellationToken);

    Task InitReportDeviceParamsAsync(CancellationToken cancellationToken);

    Task UpdateDeviceCustomPropsAsync(List<TwinReportedCustomProp> customProps, CancellationToken cancellationToken = default);
}