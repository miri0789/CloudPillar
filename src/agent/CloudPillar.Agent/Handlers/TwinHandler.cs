using System.Runtime.InteropServices;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Newtonsoft.Json;


namespace CloudPillar.Agent.Handlers;

public class TwinHandler: ITwinHandler
{
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    public TwinHandler(IDeviceClientWrapper deviceClientWrapper,
    IEnvironmentsWrapper environmentsWrapper)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);

        _deviceClientWrapper = deviceClientWrapper;
    }

    public async Task GetTwinReport()
    {
        var twin = await _deviceClientWrapper.GetTwinAsync();
        string reportJson = twin.Properties.Reported.ToJson();
        var twinReport = JsonConvert.DeserializeObject<TwinReport>(reportJson);
    }


    public async Task UpdateDeviceState(DeviceStateType deviceState)
    {
        await _deviceClientWrapper.UpdateReportedPropertiesAsync("supportedShells", GetSupportedShells().ToArray());
        await _deviceClientWrapper.UpdateReportedPropertiesAsync("agentPlatform", RuntimeInformation.OSDescription);
        await _deviceClientWrapper.UpdateReportedPropertiesAsync("deviceState", deviceState);
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