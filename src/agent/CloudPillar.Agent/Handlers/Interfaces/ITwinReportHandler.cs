using CloudPillar.Agent.Entities;
using Microsoft.Azure.Devices.Shared;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinReportHandler
{
    void SetReportProperties(ActionToReport actionToReport, StatusType status, string? resultCode = null, string? resultText = null, string periodicFileName = "");
    string GetPeriodicReportedKey(PeriodicUploadAction periodicUploadAction, string periodicFileName = "");
    TwinActionReported GetActionToReport(ActionToReport actionToReport, string periodicFileName = "");

    Task UpdateReportedChangeSpecAsync(TwinReportedChangeSpec? changeSpec, string changeSpecKey, CancellationToken cancellationToken);
    Task<Twin> SetTwinReported(CancellationToken cancellationToken);
    Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReported, CancellationToken cancellationToken);

    Task UpdateDeviceStateAsync(DeviceStateType deviceState, CancellationToken cancellationToken);
    Task<DeviceStateType?> GetDeviceStateAsync(CancellationToken cancellationToken = default);

    Task UpdateDeviceStateAfterServiceRestartAsync(DeviceStateType? deviceState, CancellationToken cancellationToken);
    Task<DeviceStateType?> GetDeviceStateAfterServiceRestartAsync(CancellationToken cancellationToken = default);

    Task UpdateDeviceSecretKeyAsync(string secretKey, CancellationToken cancellationToken);

    Task UpdateDeviceCertificateValidity(int CertificateExpiredDays, CancellationToken cancellationToken);

    Task InitReportDeviceParamsAsync(CancellationToken cancellationToken);

    Task UpdateDeviceCustomPropsAsync(Dictionary<string, object> customProps, CancellationToken cancellationToken = default);
}