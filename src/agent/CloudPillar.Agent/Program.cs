using System.Runtime.InteropServices;
using CloudPillar.Agent;
using Shared.Logger;

var builder = LoggerHostCreator.Configure("Agent API", WebApplication.CreateBuilder(args));
bool runAsService = args != null && args.Length > 0 && args[0] == "--winsrv";
if (runAsService && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    
    InstallWindowsService(); 
}
// else
// {

// }

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "CP_Agent_Service11_new";
});

builder.Services.AddHostedService<AgentService>();

var app = builder.Build();


app.Run();

    
public partial class Program
    {
        // P/Invoke declarations
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr CreateService(IntPtr hSCManager, string lpServiceName, string lpDisplayName, uint dwDesiredAccess, uint dwServiceType, uint dwStartType, uint dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        // Constants
        private const uint SC_MANAGER_CREATE_SERVICE = 0x0002;
        private const uint SERVICE_WIN32_SHARE_PROCESS = 0x00000020;
        private const uint SERVICE_AUTO_START = 0x00000002;
        private const uint SERVICE_ERROR_NORMAL = 0x00000001;

        public static void InstallWindowsService()
    {
        IntPtr scm = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);
        if (scm == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        //string exePath = AppDomain.CurrentDomain.BaseDirectory + "CloudPillar.Agent.exe" ;
        //string exePath = @"C:\Biosense\Repo\CloudPillar\src\agent\CloudPillar.Agent\publish\win-x64\CloudPillar.Agent.exe --winsrv";

        IntPtr svc = CreateService(scm, "CP_Agent_Service11_new", "Cloud Pillar Agent Service1 new", SC_MANAGER_CREATE_SERVICE, SERVICE_WIN32_SHARE_PROCESS, SERVICE_AUTO_START, SERVICE_ERROR_NORMAL, exePath, null, IntPtr.Zero, null, null, null);

        if (svc == IntPtr.Zero)
        {
            CloseServiceHandle(scm);
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        CloseServiceHandle(svc);
        CloseServiceHandle(scm);
    }
}
