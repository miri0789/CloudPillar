using System.Runtime.InteropServices;
using System.ServiceProcess;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using System.ComponentModel;

namespace CloudPillar.Agent.Wrappers
{
    public class WindowsServiceWrapper: IWindowsServiceWrapper
    {
        private readonly ILoggerHandler _logger;
        AuthenticationSettings _authenticationSettings;

        // P/Invoke declarations
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr CreateService(IntPtr hSCManager, string lpServiceName, string lpDisplayName, uint dwDesiredAccess, uint dwServiceType, uint dwStartType, uint dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr OpenService(IntPtr hSCManager, string lpServiceName, int dwDesiredAccess);
        
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool DeleteService(IntPtr hService);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool StartService(IntPtr hService, int dwNumServiceArgs, string lpServiceArgVectors);

        // Constants
        private const uint SC_MANAGER_CREATE_SERVICE = 0x0002;
        private const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
        private const uint SERVICE_WIN32_OWN_PROCESS = 0x00000010;
        private const uint SERVICE_AUTO_START = 0x00000002;
        private const uint SERVICE_ERROR_NORMAL = 0x00000001;
        private const int SERVICE_ALL_ACCESS = 0xF01FF;
        private const int DELETE = 0x10000;
        

        public WindowsServiceWrapper(ILoggerHandler logger, IOptions<AuthenticationSettings> authenticationSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authenticationSettings = authenticationSettings.Value ?? throw new ArgumentNullException(nameof(authenticationSettings));
    }

        public void InstallWindowsService(string serviceName, string workingDirectory)
        {
            try
                {
                if (ServiceExists(serviceName))
                {
                    if (IsServiceRunning(serviceName))
                    {
                        if (StopService(serviceName))
                        {
                            _logger.Info("Service stopped successfully.");
                        }
                        else
                        {
                            _logger.Error("Failed to stop service.");
                        }
                    }
                    // delete existing service
                    if (DeleteExistingService(serviceName))
                    {
                        _logger.Info("Service deleted successfully.");
                    }
                    else
                    {
                        _logger.Error("Failed to delete service.");
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                
                // Service doesn't exist, so create and start it
                if (CreateAndStartService(serviceName, workingDirectory))
                {
                    _logger.Info("Service created and started successfully.");
                }
                else
                {
                    _logger.Error("Failed to create and start service.");
                }
            }
            catch(Win32Exception ex)
            {
                throw new Exception($"Failed to start service {ex}");
            }
            
        }

        private bool DeleteExistingService(string serviceName)
        {
            // Open the service control manager
            IntPtr scmHandle = OpenSCManager(_authenticationSettings.Domain, null, SC_MANAGER_ALL_ACCESS);
            if (scmHandle == IntPtr.Zero)
            {
                return false;
            }

            // Open the existing service
            IntPtr serviceHandle = OpenService(scmHandle, serviceName, DELETE);
            if (serviceHandle == IntPtr.Zero)
            {
                CloseServiceHandle(scmHandle);
                return false;
            }

            // Delete the service
            bool success = DeleteService(serviceHandle);

            // Close the handles
            CloseServiceHandle(serviceHandle);
            CloseServiceHandle(scmHandle);

            return success;
        }
        private bool CreateAndStartService(string serviceName, string workingDirectory)
        {
            IntPtr scm = OpenSCManager(_authenticationSettings.Domain, null, SC_MANAGER_CREATE_SERVICE);
            if (scm == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            string exePath = $"{System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName} {workingDirectory}";
            string userName = string.IsNullOrWhiteSpace(_authenticationSettings.UserName)
                            ? null
                            : $"{(string.IsNullOrWhiteSpace(_authenticationSettings.Domain) ? "." : _authenticationSettings.Domain)}\\{_authenticationSettings.UserName}";
            IntPtr svc = CreateService(scm, serviceName, serviceName, SERVICE_ALL_ACCESS, SERVICE_WIN32_OWN_PROCESS, SERVICE_AUTO_START, SERVICE_ERROR_NORMAL, exePath, null, IntPtr.Zero, null, userName, _authenticationSettings.UserPassword);

            if (svc == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                CloseServiceHandle(scm);
                throw new Win32Exception(error);
            }

            bool success = StartService(svc, 0, null);
            if(success == false)
            {
                int error = Marshal.GetLastWin32Error();
                CloseServiceHandle(svc);
                CloseServiceHandle(scm);
                throw new Win32Exception(error);
            }

            CloseServiceHandle(svc);
            
            CloseServiceHandle(scm);
            return success;
        }

        private bool ServiceExists(string serviceName)
        {
            // Check if the service exists
            ServiceController[] services = ServiceController.GetServices();
            return services.Any(service => service.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsServiceRunning(string serviceName)
        {
            using (ServiceController serviceController = new ServiceController(serviceName))
            {
                return serviceController.Status == ServiceControllerStatus.Running;
            }
        }

        private bool StopService(string serviceName)
        {
            try
            {
                using (ServiceController serviceController = new ServiceController(serviceName))
                {
                    if (serviceController.Status != ServiceControllerStatus.Stopped)
                    {
                        serviceController.Stop();
                        serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Info($"Error stopping service: {ex.Message}");
                return false;
            }
        }
    }
}