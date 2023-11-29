using System.Runtime.InteropServices;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Newtonsoft.Json;
using System.Reflection;
using CloudPillar.Agent.Entities;
using Shared.Logger;
using Newtonsoft.Json.Converters;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;

namespace CloudPillar.Agent.Handlers;


public class TwinHandler : ITwinHandler
{
    private readonly IDeviceClientWrapper _deviceClient;
    private readonly IFileDownloadHandler _fileDownloadHandler;
    private readonly IFileUploaderHandler _fileUploaderHandler;
    private readonly ITwinActionsHandler _twinActionsHandler;
    private readonly IRuntimeInformationWrapper _runtimeInformationWrapper;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly IEnumerable<ShellType> _supportedShells;
    private readonly IStrictModeHandler _strictModeHandler;
    private readonly StrictModeSettings _strictModeSettings;
    private readonly ILoggerHandler _logger;
    private static Twin _latestTwin { get; set; }

    public TwinHandler(IDeviceClientWrapper deviceClientWrapper,
                       IFileDownloadHandler fileDownloadHandler,
                       IFileUploaderHandler fileUploaderHandler,
                       ITwinActionsHandler twinActionsHandler,
                       ILoggerHandler loggerHandler,
                       IRuntimeInformationWrapper runtimeInformationWrapper,
                       IStrictModeHandler strictModeHandler,
                       IFileStreamerWrapper fileStreamerWrapper,
                       IOptions<StrictModeSettings> strictModeSettings)
    {
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _fileDownloadHandler = fileDownloadHandler ?? throw new ArgumentNullException(nameof(fileDownloadHandler));
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _twinActionsHandler = twinActionsHandler ?? throw new ArgumentNullException(nameof(twinActionsHandler));
        _runtimeInformationWrapper = runtimeInformationWrapper ?? throw new ArgumentNullException(nameof(runtimeInformationWrapper));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _strictModeHandler = strictModeHandler ?? throw new ArgumentNullException(nameof(strictModeHandler));
        _supportedShells = GetSupportedShells();
        _strictModeSettings = strictModeSettings.Value ?? throw new ArgumentNullException(nameof(strictModeSettings));
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
    }

    public async Task OnDesiredPropertiesUpdateAsync(CancellationToken cancellationToken)
    {
        try
        {
            var twin = await _deviceClient.GetTwinAsync(cancellationToken);
            string reportedJson = twin.Properties.Reported.ToJson();
            var twinReported = JsonConvert.DeserializeObject<TwinReported>(reportedJson);
            string desiredJson = twin.Properties.Desired.ToJson();
            var twinDesired = JsonConvert.DeserializeObject<TwinDesired>(desiredJson,
                    new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> {
                            new TwinDesiredConverter(), new TwinActionConverter() }
                    });


            var actions = await GetActionsToExecAsync(twinDesired, twinReported);
            _logger.Info($"HandleTwinActions {actions.Count()} actions to exec");
            if (actions.Count() > 0)
            {
                await HandleTwinActionsAsync(actions, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"OnDesiredPropertiesUpdate failed", ex);
        }
    }
    public async Task HandleTwinActionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await OnDesiredPropertiesUpdateAsync(cancellationToken);
            DesiredPropertyUpdateCallback callback = async (desiredProperties, userContext) =>
                            {
                                _logger.Info($"Desired properties were updated.");
                                await OnDesiredPropertiesUpdateAsync(cancellationToken);
                            };
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(callback, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"HandleTwinActionsAsync failed", ex);
        }

    }

    public async Task UpdateDeviceStateAsync(DeviceStateType deviceState)
    {
        try
        {
            var deviceStateKey = nameof(TwinReported.DeviceState);
            await _deviceClient.UpdateReportedPropertiesAsync(deviceStateKey, deviceState.ToString());
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
            return reported.DeviceState;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetDeviceStateAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task UpdateDeviceSecretKeyAsync(string secretKey)
    {
        try
        {
            var deviceSecretKey = nameof(TwinReported.SecretKey);
            await _deviceClient.UpdateReportedPropertiesAsync(deviceSecretKey, secretKey);
            _logger.Info($"UpdateDeviceSecretKeyAsync success");
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateDeviceSecretKeyAsync failed", ex);
        }
    }

    public async Task InitReportDeviceParamsAsync()
    {
        try
        {
            var supportedShellsKey = nameof(TwinReported.SupportedShells);
            await _deviceClient.UpdateReportedPropertiesAsync(supportedShellsKey, _supportedShells);
            var agentPlatformKey = nameof(TwinReported.AgentPlatform);
            await _deviceClient.UpdateReportedPropertiesAsync(agentPlatformKey, _runtimeInformationWrapper.GetOSDescription());
            _logger.Info("InitReportedDeviceParams success");
        }
        catch (Exception ex)
        {
            _logger.Error($"InitReportedDeviceParams failed: {ex.Message}");
        }
    }

    public async Task<string> GetTwinJsonAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var twin = await _deviceClient.GetTwinAsync(cancellationToken);
            if (twin != null)
            {
                return twin.ToJson();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetTwinJsonAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task SaveLastTwinAsync(CancellationToken cancellationToken = default)
    {
        var twin = await _deviceClient.GetTwinAsync(cancellationToken);
        _latestTwin = twin;
    }

    public async Task<string> GetLatestTwinAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_latestTwin != null)
            {
                return _latestTwin.ToJson();
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetLatestTwinAsync failed: {ex.Message}");
            throw;
        }
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
                var twinReportedCustom = twinReported.Custom ?? new List<TwinReportedCustomProp>();
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
                await _deviceClient.UpdateReportedPropertiesAsync(deviceCustomProps, twinReportedCustom);
            }
            _logger.Info($"UpdateDeviceSecretKeyAsync success");
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateDeviceCustomPropsAsync failed", ex);
        }
    }

    private async Task HandleTwinActionsAsync(IEnumerable<ActionToReport> actions, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var action in actions)
            {
                switch (action.TwinAction.Action)
                {
                    case TwinActionType.SingularDownload:
                        var fileName = await HandleStrictMode(action, cancellationToken);
                        if (string.IsNullOrEmpty(fileName)) { continue; }
                        await _fileDownloadHandler.InitFileDownloadAsync((DownloadAction)action.TwinAction, action, cancellationToken);
                        break;

                    case TwinActionType.SingularUpload:
                        var uploadFileName = await HandleStrictMode(action, cancellationToken);
                        if (string.IsNullOrEmpty(uploadFileName)) { continue; }
                        await _fileUploaderHandler.FileUploadAsync((UploadAction)action.TwinAction, action, uploadFileName, cancellationToken);
                        break;
                    case TwinActionType.ExecuteOnce:
                        if (_strictModeSettings.StrictMode)
                        {
                            _logger.Info("Strict Mode is active, Bash/PowerShell actions are not allowed");
                            await UpdateTwinReportedAsync(action, StatusType.Failed, ResultCode.StrictModeBashPowerShell.ToString(), cancellationToken);
                            continue;
                        }
                        break;
                    default:
                        await UpdateTwinReportedAsync(action, StatusType.Failed, ResultCode.NotFound.ToString(), cancellationToken);
                        _logger.Info($"HandleTwinActions, no handler found guid: {action.TwinAction.ActionId}");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"HandleTwinActions failed", ex);
        }
    }
    private async Task<string> HandleStrictMode(ActionToReport action, CancellationToken cancellationToken)
    {
        var fileName = string.Empty;
        try
        {
            var actionFileName = GetFileNameByAction(action);

            fileName = _strictModeHandler.ReplaceRootById(action.TwinAction.Action.Value, actionFileName) ?? actionFileName;
            _strictModeHandler.CheckFileAccessPermissions(action.TwinAction.Action.Value, fileName);

        }
        catch (Exception ex)
        {
            await UpdateTwinReportedAsync(action, StatusType.Failed, ex.Message, cancellationToken);
            return string.Empty;
        }
        return fileName;
    }
    private async Task UpdateTwinReportedAsync(ActionToReport action, StatusType statusType, string resultCode, CancellationToken cancellationToken)
    {
        action.TwinReport.Status = statusType;
        action.TwinReport.ResultCode = resultCode;
        await _twinActionsHandler.UpdateReportActionAsync(new List<ActionToReport>() { action }, cancellationToken);
    }
    private string GetFileNameByAction(ActionToReport action)
    {
        string fileName = string.Empty;
        switch (action.TwinAction.Action)
        {
            case TwinActionType.SingularDownload:
                var destination = ((DownloadAction)action.TwinAction).DestinationPath;
                var source = ((DownloadAction)action.TwinAction).Source;

                fileName = destination + source;
                break;
            case TwinActionType.SingularUpload:
                fileName = ((UploadAction)action.TwinAction).FileName;
                break;
        }
        return fileName;
    }

    private async Task<IEnumerable<ActionToReport>> GetActionsToExecAsync(TwinDesired twinDesired, TwinReported twinReported)
    {
        try
        {
            var isReportedChanged = false;
            var actions = new List<ActionToReport>();
            twinReported.ChangeSpec ??= new TwinReportedChangeSpec();
            if (twinReported.ChangeSpec.Patch == null || twinReported.ChangeSpec.Id != twinDesired.ChangeSpec.Id)
            {
                twinReported.ChangeSpec.Patch = new TwinReportedPatch();
                twinReported.ChangeSpec.Id = twinDesired.ChangeSpec.Id;
                isReportedChanged = true;
            }

            PropertyInfo[] properties = typeof(TwinPatch).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    var desiredValue = (TwinAction[])property.GetValue(twinDesired.ChangeSpec.Patch);
                    if (desiredValue?.Length > 0)
                    {
                        var reportedProp = typeof(TwinReportedPatch).GetProperty(property.Name);
                        var reportedValue = ((TwinActionReported[])(reportedProp.GetValue(twinReported.ChangeSpec.Patch) ?? new TwinActionReported[0])).ToList();

                        while (reportedValue.Count < desiredValue.Length)
                        {
                            reportedValue.Add(
                                new TwinActionReported() { Status = StatusType.Pending });
                            isReportedChanged = true;
                        }

                        reportedProp.SetValue(twinReported.ChangeSpec.Patch, reportedValue.ToArray());
                        actions.AddRange(desiredValue
                           .Select((item, index) => new ActionToReport
                           {
                               ReportPartName = property.Name,
                               ReportIndex = index,
                               TwinAction = item,
                               TwinReport = reportedValue[index]
                           })
                           .Where((item, index) => reportedValue[index].Status == StatusType.Pending));

                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"GetActionsToExec failed , desired part: {property.Name} exception: {ex.Message}");
                    continue;
                }
            }
            if (isReportedChanged)
            {
                await _twinActionsHandler.UpdateReportedChangeSpecAsync(twinReported.ChangeSpec);
            }
            return actions;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetActionsToExec failed: {ex.Message}");
            return null;
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
}