using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Newtonsoft.Json;
using CloudPillar.Agent.Entities;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Utilities;
using System.Runtime.InteropServices;

namespace CloudPillar.Agent.Handlers;
public class TwinReportHandler : ITwinReportHandler
{
    private readonly IDeviceClientWrapper _deviceClient;
    private readonly ILoggerHandler _logger;
    private readonly IRuntimeInformationWrapper _runtimeInformationWrapper;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;

    public TwinReportHandler(IDeviceClientWrapper deviceClientWrapper,
     ILoggerHandler loggerHandler,
     IRuntimeInformationWrapper runtimeInformationWrapper,
     IFileStreamerWrapper fileStreamerWrapper)
    {
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _runtimeInformationWrapper = runtimeInformationWrapper ?? throw new ArgumentNullException(nameof(runtimeInformationWrapper));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
    }

    public void SetReportProperties(ActionToReport actionToReport, StatusType status, string? resultCode = null, string? resultText = null, string periodicFileName = "")
    {
        var twinReport = GetActionToReport(actionToReport, periodicFileName);

        twinReport.Status = status;
        twinReport.ResultText = resultCode;
        twinReport.ResultCode = resultText;
    }
    public string GetPeriodicReportedKey(PeriodicUploadAction periodicUploadAction, string periodicFileName = "")
    {
        return periodicFileName.Substring(periodicUploadAction.DirName.Length + 1)
                   .Replace(FileConstants.SEPARATOR, FileConstants.DOUBLE_SEPARATOR)
                   .Replace(FileConstants.DOT, "_")
                   .Replace(" ", "_");

    }

    public TwinActionReported GetActionToReport(ActionToReport actionToReport, string periodicFileName = "")
    {
        if (actionToReport.TwinAction is PeriodicUploadAction periodicUploadAction &&
        !string.IsNullOrWhiteSpace(periodicFileName) && periodicUploadAction.DirName != periodicFileName)
        {
            var key = GetPeriodicReportedKey(periodicUploadAction, periodicFileName);
            return actionToReport.TwinReport.PeriodicReported![key];
        }
        return actionToReport.TwinReport;

    }

    public async Task UpdateReportedChangeSpecAsync(TwinReportedChangeSpec changeSpec, TwinPatchChangeSpec changeSpecKey, CancellationToken cancellationToken)
    {
        var changeSpecJson = JObject.Parse(JsonConvert.SerializeObject(changeSpec,
          Formatting.None,
          new JsonSerializerSettings
          {
              ContractResolver = new CamelCasePropertyNamesContractResolver(),
              Converters = { new StringEnumConverter() },
              Formatting = Formatting.Indented,
              NullValueHandling = NullValueHandling.Ignore
          }));
        await _deviceClient.UpdateReportedPropertiesAsync(changeSpecKey.ToString(), changeSpecJson, cancellationToken);
    }

    public async Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReported, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var twin = await _deviceClient.GetTwinAsync(cancellationToken);
                string reportedJson = twin.Properties.Reported.ToJson();
                var twinReported = JsonConvert.DeserializeObject<TwinReported>(reportedJson);

                var actionForDetails = actionsToReported.FirstOrDefault(x => !string.IsNullOrEmpty(x.ReportPartName));
                if (actionForDetails == null) return;
                TwinPatchChangeSpec changeSpecKey = actionForDetails.ChangeSpecKey;
                TwinReportedChangeSpec twinReportedChangeSpec = twinReported.GetReportedChangeSpecByKey(changeSpecKey);


                actionsToReported.ToList().ForEach(actionToReport =>
                {
                    if (string.IsNullOrEmpty(actionToReport.ReportPartName)) return;
                    var reportedProp = typeof(TwinReportedPatch).GetProperty(actionToReport.ReportPartName);
                    var reportedValue = (TwinActionReported[])reportedProp?.GetValue(twinReportedChangeSpec.Patch)!;
                    reportedValue[actionToReport.ReportIndex] = actionToReport.TwinReport;
                    reportedProp?.SetValue(twinReportedChangeSpec.Patch, reportedValue);
                });
                await UpdateReportedChangeSpecAsync(twinReportedChangeSpec, changeSpecKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error($"UpdateReportedAction failed: {ex.Message}");
            }

        }
    }

    public async Task UpdateDeviceStateAsync(DeviceStateType deviceState, CancellationToken cancellationToken)
    {
        try
        {
            var deviceStateKey = nameof(TwinReported.DeviceState);
            await _deviceClient.UpdateReportedPropertiesAsync(deviceStateKey, deviceState.ToString(), cancellationToken);
            _logger.Info($"UpdateDeviceStateAsync success");
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateDeviceStateAsync failed: {ex.Message}");
        }
    }

    public async Task<DeviceStateType?> GetDeviceStateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var twin = await _deviceClient.GetTwinAsync(cancellationToken);
            var reported = JsonConvert.DeserializeObject<TwinReported>(twin.Properties.Reported.ToJson());
            return reported?.DeviceState;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetDeviceStateAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task UpdateDeviceStateAfterServiceRestartAsync(DeviceStateType? deviceState, CancellationToken cancellationToken)
    {
        try
        {
            var deviceStateAfterServiceRestartKey = nameof(TwinReported.DeviceStateAfterServiceRestart);
            await _deviceClient.UpdateReportedPropertiesAsync(deviceStateAfterServiceRestartKey, deviceState?.ToString(), cancellationToken);
            _logger.Info($"UpdateDeviceStateAfterServiceRestartAsync success");
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateDeviceStateAfterServiceRestartAsync failed: {ex.Message}");
        }
    }

    public async Task<DeviceStateType?> GetDeviceStateAfterServiceRestartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var twin = await _deviceClient.GetTwinAsync(cancellationToken);
            var reported = JsonConvert.DeserializeObject<TwinReported>(twin.Properties.Reported.ToJson());
            return reported?.DeviceStateAfterServiceRestart;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetDeviceStateAfterServiceRestartAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task UpdateDeviceSecretKeyAsync(string secretKey, CancellationToken cancellationToken)
    {
        try
        {
            var deviceSecretKey = nameof(TwinReported.SecretKey);
            await _deviceClient.UpdateReportedPropertiesAsync(deviceSecretKey, secretKey, cancellationToken);
            _logger.Info($"UpdateDeviceSecretKeyAsync success");
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateDeviceSecretKeyAsync failed message: {ex.Message}");
        }
    }

    public async Task UpdateDeviceCertificateValidity(int CertificateExpiredDays, CancellationToken cancellationToken)
    {
        try
        {
            var certificateValidity = new CertificateValidity()
            {
                CreationDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(CertificateExpiredDays)
            };
            var certificateDates = nameof(TwinReported.CertificateValidity);
            await _deviceClient.UpdateReportedPropertiesAsync(certificateDates, certificateValidity, cancellationToken);
            _logger.Info($"UpdateDeviceCertificateValidity success");
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateDeviceCertificateValidity failed message: {ex.Message}");
        }
    }

    public async Task InitReportDeviceParamsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var supportedShellsKey = nameof(TwinReported.SupportedShells);
            await _deviceClient.UpdateReportedPropertiesAsync(supportedShellsKey, GetSupportedShells(), cancellationToken);
            var agentPlatformKey = nameof(TwinReported.AgentPlatform);
            await _deviceClient.UpdateReportedPropertiesAsync(agentPlatformKey, _runtimeInformationWrapper.GetOSDescription(), cancellationToken);
            _logger.Info("InitReportedDeviceParams success");
        }
        catch (Exception ex)
        {
            _logger.Error($"InitReportedDeviceParams failed: {ex.Message}");
        }
    }

    private IEnumerable<ShellType> GetSupportedShells()
    {
        const string windowsBashPath = @"C:\Windows\System32\wsl.exe";
        const string linuxPsPath1 = @"/usr/bin/pwsh";
        const string linuxPsPath2 = @"/usr/local/bin/pwsh";

        var supportedShells = new List<ShellType>();
        if (_runtimeInformationWrapper.IsOSPlatform(OSPlatform.Windows))
        {
            supportedShells.Add(ShellType.Cmd);
            supportedShells.Add(ShellType.Powershell);
            // Check if WSL is installed
            if (_fileStreamerWrapper.FileExists(windowsBashPath))
            {
                supportedShells.Add(ShellType.Bash);
            }
        }
        else if (_runtimeInformationWrapper.IsOSPlatform(OSPlatform.Linux) || _runtimeInformationWrapper.IsOSPlatform(OSPlatform.OSX))
        {
            supportedShells.Add(ShellType.Bash);

            // Add PowerShell if it's installed on Linux or macOS
            if (_fileStreamerWrapper.FileExists(linuxPsPath1) || _fileStreamerWrapper.FileExists(linuxPsPath2))
            {
                supportedShells.Add(ShellType.Powershell);
            }
        }
        return supportedShells;
    }

    public async Task UpdateDeviceCustomPropsAsync(List<TwinReportedCustomProp> customProps, CancellationToken cancellationToken = default)
    {
        try
        {
            if (customProps != null)
            {
                var twin = await _deviceClient.GetTwinAsync(cancellationToken);
                string reportedJson = twin.Properties.Reported.ToJson();
                var twinReported = JsonConvert.DeserializeObject<TwinReported>(reportedJson);
                var twinReportedCustom = twinReported?.Custom ?? new List<TwinReportedCustomProp>();
                foreach (var item in customProps)
                {
                    var existingItem = twinReportedCustom.FirstOrDefault(x => x.Name == item.Name);

                    if (existingItem != null)
                    {
                        existingItem.Value = item.Value;
                    }
                    else
                    {
                        twinReportedCustom.Add(item);
                    }
                }
                var deviceCustomProps = nameof(TwinReported.Custom);
                await _deviceClient.UpdateReportedPropertiesAsync(deviceCustomProps, twinReportedCustom, cancellationToken);
            }
            _logger.Info($"UpdateDeviceSecretKeyAsync success");
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateDeviceCustomPropsAsync failed message: {ex.Message}");
        }
    }
}