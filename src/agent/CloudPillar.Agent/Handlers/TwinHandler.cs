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
    private readonly IDeviceClientWrapper _deviceClient;
    private readonly IFileDownloadHandler _fileDownloadHandler;
    public TwinHandler(IDeviceClientWrapper deviceClientWrapper,
    IFileDownloadHandler fileDownloadHandler)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(fileDownloadHandler);

        _deviceClient = deviceClientWrapper;
        _fileDownloadHandler = fileDownloadHandler;
        
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
            Console.WriteLine($"HandleTwinActions {actions.Count()} actions to exec");
            if (actions.Count() > 0)
            {
                await HandleTwinActionsAsync(actions);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HandleTwinActions failed: {ex.Message}");
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
            var tasks = new List<Task>();
            foreach (var action in actions)
            {
                switch (action.TwinAction)
                {
                    case DownloadAction downloadAction:
                        tasks.Add(_fileDownloadHandler.InitFileDownloadAsync(action));
                        action.Status = StatusType.InProgress;
                        break;

                    case UploadAction uploadAction:
                        // Handle UploadAction
                        break;

                    default:
                        action.Status = StatusType.Failed;
                        action.ResultCode = ResultCode.NotFound.ToString();
                        Console.WriteLine($"HandleTwinActions, no handler found guid: {action.TwinAction.ActionGuid}");
                        break;
                }
            }
            await UpdateReportActionAsync(actions);
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"HandleTwinActions failed: {ex.Message}");
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
                           .Where((item, index) => reportedValue[index].Status == StatusType.Pending)
                           .Select((item, index) => new ActionToReport
                           {
                               TwinPartName = property.Name,
                               TwinReportIndex = index,
                               TwinAction = item
                           }));

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetActionsToExec failed , desired part: {property.Name} exception: {ex.Message}");
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
            Console.WriteLine($"GetActionsToExec failed: {ex.Message}");
            return null;
        }
    }

    public async Task UpdateDeviceStateAsync(DeviceStateType deviceState)
    {
        try
        {
            var deviceStateKey = nameof(TwinReported.DeviceState);
            await _deviceClient.UpdateReportedPropertiesAsync(deviceStateKey, deviceState);
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
            var supportedShellsKey = nameof(TwinReported.SupportedShells);
            await _deviceClient.UpdateReportedPropertiesAsync(supportedShellsKey, GetSupportedShells().ToArray());
            var agentPlatformKey = nameof(TwinReported.AgentPlatform);
            await _deviceClient.UpdateReportedPropertiesAsync(agentPlatformKey, RuntimeInformation.OSDescription);
            Console.WriteLine("InitReportedDeviceParams success");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"InitReportedDeviceParams failed: {ex.Message}");
        }
    }

    public async Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReported)
    {
        try
        {
            var twin = await _deviceClient.GetTwinAsync();
            string reportedJson = twin.Properties.Reported.ToJson();
            var twinReported = JsonConvert.DeserializeObject<TwinReported>(reportedJson);
            actionsToReported.ToList().ForEach(ActionToReport =>
            {
                var reportedProp = typeof(TwinReportedPatch).GetProperty(ActionToReport.TwinPartName);
                var reportedValue = (TwinActionReported[])reportedProp.GetValue(twinReported.ChangeSpec.Patch);
                reportedValue[ActionToReport.TwinReportIndex] = new TwinActionReported()
                {
                    Status = ActionToReport.Status,
                    Progress = ActionToReport.Progress
                };
                reportedProp.SetValue(twinReported.ChangeSpec.Patch, reportedValue);
            });
            await UpdateReportedChangeSpecAsync(twinReported.ChangeSpec);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateReportedAction failed: {ex.Message}");
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