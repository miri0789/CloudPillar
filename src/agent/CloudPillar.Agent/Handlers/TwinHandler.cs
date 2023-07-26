using System.Runtime.InteropServices;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Newtonsoft.Json;
using System.Reflection;
using CloudPillar.Agent.Entities;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace CloudPillar.Agent.Handlers;
public class TwinHandler : ITwinHandler
{
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly IFileDownloadHandler _fileDownloadHandler;
    public TwinHandler(IDeviceClientWrapper deviceClientWrapper,
    IFileDownloadHandler fileDownloadHandler)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(fileDownloadHandler);

        _deviceClientWrapper = deviceClientWrapper;
        _fileDownloadHandler = fileDownloadHandler;
    }

    public async Task HandleTwinActionsAsync()
    {
        try
        {
            var twin = await _deviceClientWrapper.GetTwinAsync();
            string reportJson = twin.Properties.Reported.ToJson();
            var twinReport = JsonConvert.DeserializeObject<TwinReport>(reportJson);
            string desiredJson = twin.Properties.Desired.ToJson();
            var twinDesired = JsonConvert.DeserializeObject<TwinDesired>(desiredJson,
                    new JsonSerializerSettings
                    {
                        Converters = new List<JsonConverter> {
                            new TwinDesiredConverter(), new TwinActionConverter() }
                    });
                    
            if (twinDesired.ChangeSpec != null)
            {
                var actions = await GetActionsToExecAsync(twinDesired, twinReport);
                if (actions?.Count() > 0)
                {
                    Console.WriteLine($"HandleTwinActions {actions.Count()} actions to exec");
                    await HandleTwinActionsAsync(actions);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HandleTwinActions failed: {ex.Message}");
        }

    }

    private async Task UpdateReportChangeSpecAsync(TwinReportChangeSpec changeSpec)
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
        var changeSpecKey = LowerFirtLetter(nameof(TwinReport.ChangeSpec));
        await _deviceClientWrapper.UpdateReportedPropertiesAsync(changeSpecKey, changeSpecJson);

    }

    private async Task HandleTwinActionsAsync(IEnumerable<ActionToReport> actions)
    {
        try
        {
            foreach (var action in actions)
            {
                switch (action.TwinAction.Action)
                {
                    case TwinActionType.SingularDownload:
                        await _fileDownloadHandler.InitFileDownloadAsync(action);
                        Console.WriteLine($"HandleTwinAction, download file: {((DownloadAction)action.TwinAction).Source} {actions.Count()} init");
                        break;
                    default:
                        Console.WriteLine($"HandleTwinActions, no handler found guid: {action.TwinAction.ActionGuid}");
                        continue;
                }
                action.Status = StatusType.InProgress;
                await UpdateReportActionAsync(action);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HandleTwinActions failed: {ex.Message}");
        }
    }

    private async Task<IEnumerable<ActionToReport>> GetActionsToExecAsync(TwinDesired twinDesired, TwinReport twinReport)
    {
        try
        {
            var isReportChanged = false;
            var actions = new List<ActionToReport>();
            twinReport.ChangeSpec ??= new TwinReportChangeSpec();
            if (twinReport.ChangeSpec.Patch == null || twinReport.ChangeSpec.Id != twinDesired.ChangeSpec.Id)
            {
                twinReport.ChangeSpec.Patch = new TwinReportPatch();
                twinReport.ChangeSpec.Id = twinDesired.ChangeSpec.Id;
                isReportChanged = true;
            }

            PropertyInfo[] properties = typeof(TwinPatch).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                var desiredValue = (TwinAction[])property.GetValue(twinDesired.ChangeSpec.Patch);
                if (desiredValue?.Length > 0)
                {
                    var reportProp = typeof(TwinReportPatch).GetProperty(property.Name);
                    var reportValue = ((TwinActionReport[])(reportProp.GetValue(twinReport.ChangeSpec.Patch) ?? new TwinActionReport[0])).ToList();

                    while (reportValue.Count < desiredValue.Length)
                    {
                        reportValue.Add(
                            new TwinActionReport() { Status = StatusType.Pending });
                        isReportChanged = true;
                    }

                    reportProp.SetValue(twinReport.ChangeSpec.Patch, reportValue.ToArray());
                    actions.AddRange(desiredValue
                       .Where((item, index) => reportValue[index].Status == StatusType.Pending)
                       .Select((item, index) => new ActionToReport
                       {
                           TwinPartName = property.Name,
                           TwinReportIndex = index,
                           TwinAction = item
                       }));

                }
            }
            if (isReportChanged)
            {
                await UpdateReportChangeSpecAsync(twinReport.ChangeSpec);
            }
            return actions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GetActionsToExec failed: {ex.Message}");
            return null;
        }
    }

    public async Task UpdateDeviceStateAsync(DeviceStateType deviceState)
    {
        try
        {
            var deviceStateKey = LowerFirtLetter(nameof(TwinReport.DeviceState));
            await _deviceClientWrapper.UpdateReportedPropertiesAsync(deviceStateKey, deviceState);
            Console.WriteLine($"UpdateDeviceStateAsync success");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateDeviceStateAsync failed: {ex.Message}");
        }
    }

    public async Task InitReportDeviceParamsAsync()
    {
        try
        {
            var supportedShellsKey = LowerFirtLetter(nameof(TwinReport.SupportedShells));
            await _deviceClientWrapper.UpdateReportedPropertiesAsync(supportedShellsKey, GetSupportedShells().ToArray());
            var agentPlatformKey = LowerFirtLetter(nameof(TwinReport.AgentPlatform));
            await _deviceClientWrapper.UpdateReportedPropertiesAsync(agentPlatformKey, RuntimeInformation.OSDescription);
            Console.WriteLine($"InitReportDeviceParams success");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"InitReportDeviceParams failed: {ex.Message}");
        }
    }

    private string LowerFirtLetter(string propertyName)
    {
        return char.ToLower(propertyName[0]) + propertyName.Substring(1);
    }

    public async Task UpdateReportActionAsync(ActionToReport actionToReport)
    {
        try
        {
            var twin = await _deviceClientWrapper.GetTwinAsync();
            string reportJson = twin.Properties.Reported.ToJson();
            var twinReport = JsonConvert.DeserializeObject<TwinReport>(reportJson);
            var reportProp = typeof(TwinReportPatch).GetProperty(actionToReport.TwinPartName);
            var reportValue = (TwinActionReport[])reportProp.GetValue(twinReport.ChangeSpec.Patch);
            reportValue[actionToReport.TwinReportIndex] = new TwinActionReport()
            {
                Status = actionToReport.Status,
                Progress = actionToReport.Progress
            };
            reportProp.SetValue(twinReport.ChangeSpec.Patch, reportValue);
            await UpdateReportChangeSpecAsync(twinReport.ChangeSpec);
            Console.WriteLine($"UpdateReportAction success. index: {actionToReport.TwinReportIndex} ,status: {actionToReport.Status}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateReportAction failed: {ex.Message}");
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