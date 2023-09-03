using System.Runtime.InteropServices;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Newtonsoft.Json;
using System.Reflection;
using CloudPillar.Agent.Entities;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;
public class TwinHandler : ITwinHandler
{
    private readonly IDeviceClientWrapper _deviceClient;
    private readonly IFileDownloadHandler _fileDownloadHandler;
    private readonly IEnumerable<ShellType> _supportedShells;

    private readonly ILoggerHandler _logger;
    public TwinHandler(IDeviceClientWrapper deviceClientWrapper,
                       IFileDownloadHandler fileDownloadHandler,
                       ILoggerHandler loggerHandler)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(fileDownloadHandler);

        _deviceClient = deviceClientWrapper;
        _fileDownloadHandler = fileDownloadHandler;
        _supportedShells = GetSupportedShells();
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
    }

    public async Task HandleTwinActionsAsync()
    {
        try
        {
            var twin = await _deviceClient.GetTwinAsync();
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
                await HandleTwinActionsAsync(actions);
            }
        }
        catch (Exception ex)
        {
           _logger.Error($"HandleTwinActions failed: {ex.Message}");
        }

    }

    private async Task UpdateReportedChangeSpecAsync(TwinReportedChangeSpec changeSpec)
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
        var changeSpecKey = nameof(TwinReported.ChangeSpec);
        await _deviceClient.UpdateReportedPropertiesAsync(changeSpecKey, changeSpecJson);

    }

    private async Task HandleTwinActionsAsync(IEnumerable<ActionToReport> actions)
    {
        try
        {
            foreach (var action in actions)
            {
                switch (action.TwinAction)
                {
                    case DownloadAction downloadAction:
                        await _fileDownloadHandler.InitFileDownloadAsync(downloadAction, action);
                        break;

                    case UploadAction uploadAction:
                        // Handle UploadAction
                        break;

                    default:
                        action.TwinReport.Status = StatusType.Failed;
                        action.TwinReport.ResultCode = ResultCode.NotFound.ToString();
                        _logger.Info($"HandleTwinActions, no handler found guid: {action.TwinAction.ActionId}");
                        break;
                }
                //TODO : queue - FIFO
                // https://dev.azure.com/BiosenseWebsterIs/CloudPillar/_backlogs/backlog/CloudPillar%20Team/Epics/?workitem=9782
                await UpdateReportActionAsync(new List<ActionToReport>() { action });
            }
        }
        catch (Exception ex)
        {
           _logger.Error($"HandleTwinActions failed: {ex.Message}");
        }
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
                               TwinReport = new TwinActionReported() { Status = StatusType.Pending }
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
                await UpdateReportedChangeSpecAsync(twinReported.ChangeSpec);
            }
            return actions;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetActionsToExec failed: {ex.Message}");
            return null;
        }
    }

    public async Task UpdateDeviceStateAsync(DeviceStateType deviceState)
    {
        try
        {
            var deviceStateKey = nameof(TwinReported.DeviceState);
            await _deviceClient.UpdateReportedPropertiesAsync(deviceStateKey, deviceState);
            _logger.Info($"UpdateDeviceStateAsync success");
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateDeviceStateAsync failed: {ex.Message}");
        }
    }

    public async Task InitReportDeviceParamsAsync()
    {
        try
        {
            var supportedShellsKey = nameof(TwinReported.SupportedShells);
            await _deviceClient.UpdateReportedPropertiesAsync(supportedShellsKey, _supportedShells);
            var agentPlatformKey = nameof(TwinReported.AgentPlatform);
            await _deviceClient.UpdateReportedPropertiesAsync(agentPlatformKey, RuntimeInformation.OSDescription);
            _logger.Info("InitReportedDeviceParams success");
        }
        catch (Exception ex)
        {
           _logger.Error($"InitReportedDeviceParams failed: {ex.Message}");
        }
    }

    public async Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReported)
    {
        try
        {
            var twin = await _deviceClient.GetTwinAsync();
            string reportedJson = twin.Properties.Reported.ToJson();
            var twinReported = JsonConvert.DeserializeObject<TwinReported>(reportedJson);
            actionsToReported.ToList().ForEach(actionToReport =>
            {
                var reportedProp = typeof(TwinReportedPatch).GetProperty(actionToReport.ReportPartName);
                var reportedValue = (TwinActionReported[])reportedProp.GetValue(twinReported.ChangeSpec.Patch);
                reportedValue[actionToReport.ReportIndex] = actionToReport.TwinReport;
                reportedProp.SetValue(twinReported.ChangeSpec.Patch, reportedValue);
            });
            await UpdateReportedChangeSpecAsync(twinReported.ChangeSpec);
        }
        catch (Exception ex)
        {
            _logger.Error($"UpdateReportedAction failed: {ex.Message}");
        }

    }

    public async Task<string> GetTwinJsonAsync()
    {
        try
        {
            var twin = await _deviceClient.GetTwinAsync();
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

    private IEnumerable<ShellType> GetSupportedShells()
    {
        const string windowsBashPath = @"C:\Windows\System32\wsl.exe";
        const string linuxPsPath1 = @"/usr/bin/pwsh";
        const string linuxPsPath2 = @"/usr/local/bin/pwsh";

        var supportedShells = new List<ShellType>();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            supportedShells.Add(ShellType.Cmd);
            supportedShells.Add(ShellType.Powershell);
            // Check if WSL is installed
            if (File.Exists(windowsBashPath))
            {
                supportedShells.Add(ShellType.Bash);
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            supportedShells.Add(ShellType.Bash);

            // Add PowerShell if it's installed on Linux or macOS
            if (File.Exists(linuxPsPath1) || File.Exists(linuxPsPath2))
            {
                supportedShells.Add(ShellType.Powershell);
            }
        }
        return supportedShells;
    }


}