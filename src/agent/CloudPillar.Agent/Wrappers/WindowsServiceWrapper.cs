using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CloudPillar.Agent.Wrappers
{
    public class WindowsServiceWrapper: IWindowsServiceWrapper
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
        private const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
        private const uint SERVICE_WIN32_SHARE_PROCESS = 0x00000020;
        private const uint SERVICE_AUTO_START = 0x00000002;
        private const uint SERVICE_ERROR_NORMAL = 0x00000001;
        private const uint SERVICE_ALL_ACCESS = 0xF01FF;
        

        public void InstallWindowsService()
        {
            IntPtr scm = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);
            if (scm == IntPtr.Zero)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            IntPtr svc = CreateService(scm, "CP_Agent_Service11_neww", "Cloud Pillar Agent Service1 neww", SERVICE_ALL_ACCESS, SERVICE_WIN32_SHARE_PROCESS, SERVICE_AUTO_START, SERVICE_ERROR_NORMAL, exePath, null, default, null, null, null);

            if (svc == IntPtr.Zero)
            {
                CloseServiceHandle(scm);
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            CloseServiceHandle(svc);
            
            CloseServiceHandle(scm);
        }
    }
}