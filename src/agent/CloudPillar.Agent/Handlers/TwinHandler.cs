using System.Runtime.InteropServices;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Newtonsoft.Json;
using System.Reflection;
using CloudPillar.Agent.Entities;

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

    public async Task GetTwinReportAsync()
    {
        var twin = await _deviceClientWrapper.GetTwinAsync();
        string reportJson = twin.Properties.Reported.ToJson();
        var twinReport = JsonConvert.DeserializeObject<TwinReport>(reportJson);
    }

    public async Task GetTwinActiveActions()
    {
        var twin = await _deviceClientWrapper.GetTwinAsync();
        string reportJson = twin.Properties.Reported.ToJson();
        var twinReport = JsonConvert.DeserializeObject<TwinReport>(reportJson);
        string desiredJson = twin.Properties.Desired.ToJson();
        var twinDesired = JsonConvert.DeserializeObject<TwinDesired>(reportJson);
        if (twinDesired.ChangeSpec != null)
        {
            var actionsToDo = GetActionsToDo(twinDesired, twinReport);
            if (actionsToDo.Count() > 0)
            {
                await _deviceClientWrapper.UpdateReportedPropertiesAsync(nameof(twinReport.ChangeSpec), twinReport.ChangeSpec);

            }
        }

    }

    private async Task HandleTwinActions(IEnumerable<ActionToReport> actions)
    {
        foreach (var action in actions)
        {
            switch (action.TwinAction.ActionName)
            {
                case TwinActionType.SingularDownload:
                    _fileDownloadHandler.InitFileDownloadAsync(action.TwinAction);
                    break;
                default:
                    break;
            }
        }
    }

    private IEnumerable<ActionToReport> GetActionsToDo(TwinDesired twinDesired, TwinReport twinReport)
    {
        var actions = new List<ActionToReport>();
        twinReport.ChangeSpec ??= new TwinReportChangeSpec();
        if (twinReport.ChangeSpec.Patch == null || twinReport.ChangeSpec.Id != twinDesired.ChangeSpec.Id)
        {
            twinReport.ChangeSpec.Patch = new TwinReportPatch();
            twinReport.ChangeSpec.Id = twinDesired.ChangeSpec.Id;
        }

        PropertyInfo[] properties = typeof(TwinPatch).GetProperties();
        foreach (PropertyInfo property in properties)
        {
            var desiredValue = (TwinAction[])property.GetValue(twinDesired.ChangeSpec.Patch);
            if (desiredValue?.Length > 0)
            {
                var reportProp = typeof(TwinReportPatch).GetProperty(property.Name);
                var reportValue = (TwinActionReport[])reportProp.GetValue(twinReport.ChangeSpec.Patch) ?? new TwinActionReport[0];

                while (reportValue.Length < desiredValue.Length)
                {
                    reportValue.ToList().Add(
                        new TwinActionReport() { Status = StatusType.Pending });
                }
                reportProp.SetValue(twinReport.ChangeSpec.Patch, reportValue);
                actions.Concat(desiredValue
                   .Where((item, index) => reportValue[index].Status == StatusType.Pending)
                   .Select((item, index) => new ActionToReport
                   {
                       ArrayName = property.Name,
                       index = index,
                       TwinAction = item
                   }));

            }
        }
        return actions;
    }
    public async Task UpdateDeviceStateAsync(DeviceStateType deviceState)
    {
        await _deviceClientWrapper.UpdateReportedPropertiesAsync(nameof(TwinReport.SupportedShells), GetSupportedShells().ToArray());
        await _deviceClientWrapper.UpdateReportedPropertiesAsync(nameof(TwinReport.AgentPlatform), RuntimeInformation.OSDescription);
        await _deviceClientWrapper.UpdateReportedPropertiesAsync(nameof(TwinReport.DeviceState), deviceState);
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