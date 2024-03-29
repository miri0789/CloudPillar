using System.Runtime.InteropServices;
using System.ServiceProcess;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Text;

namespace CloudPillar.Agent.Utilities
{
    public class WindowsServiceUtils: IWindowsServiceUtils
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

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool ChangeServiceConfig2(IntPtr hService, uint dwInfoLevel, [MarshalAs(UnmanagedType.Struct)] ref SERVICE_DESCRIPTION lpInfo);
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct SERVICE_DESCRIPTION
        {
            public string lpDescription;
        }

        // Constants
        private const uint SC_MANAGER_CREATE_SERVICE = 0x0002;
        private const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
        private const uint SERVICE_WIN32_OWN_PROCESS = 0x00000010;
        private const uint SERVICE_AUTO_START = 0x00000002;
        private const uint SERVICE_ERROR_NORMAL = 0x00000001;
        private const int SERVICE_ALL_ACCESS = 0xF01FF;
        private const int DELETE = 0x10000;
        private const int SERVICE_START = 0x0010;	
        const int SERVICE_CONFIG_DESCRIPTION = 1;
        

        public WindowsServiceUtils(ILoggerHandler logger, IOptions<AuthenticationSettings> authenticationSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authenticationSettings = authenticationSettings.Value ?? throw new ArgumentNullException(nameof(authenticationSettings));
    }


        public bool DeleteExistingService(string serviceName)
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
        public void CreateService(string serviceName, string workingDirectory, string serviceDescription, string? userPassword)
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
            string password = _authenticationSettings.UserPassword ?? userPassword;
            if (string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(_authenticationSettings.UserName))
            {
                Console.WriteLine($"There is no user Password in appsettings, please enter password for user {_authenticationSettings.UserName}");
                password = ReadPasswordFromConsole();
            }
            IntPtr svc = CreateService(scm, serviceName, serviceName, SERVICE_ALL_ACCESS, SERVICE_WIN32_OWN_PROCESS, SERVICE_AUTO_START, SERVICE_ERROR_NORMAL, exePath, null, IntPtr.Zero, null, userName, password);

            if (svc == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                CloseServiceHandle(scm);
                throw new Win32Exception(error);
            }
            // Add a description to the service
            var description = new SERVICE_DESCRIPTION
            {
                lpDescription = serviceDescription
            };

            if (!ChangeServiceConfig2(svc, SERVICE_CONFIG_DESCRIPTION, ref description))
            {
                _logger.Info("ChangeServiceConfig2 failed. Error code: " + Marshal.GetLastWin32Error());
            }
            else
            {
                _logger.Info("Service description added successfully.");
            }

            CloseServiceHandle(svc);
            
            CloseServiceHandle(scm);
        }

        public bool StartService(string serviceName)
        {
            // Open the service control manager
            IntPtr scmHandle = OpenSCManager(_authenticationSettings.Domain, null, SC_MANAGER_ALL_ACCESS);
            if (scmHandle == IntPtr.Zero)
            {
                return false;
            }

            // Open the existing service
            IntPtr serviceHandle = OpenService(scmHandle, serviceName, SERVICE_START);
            if (serviceHandle == IntPtr.Zero)
            {
                CloseServiceHandle(scmHandle);
                return false;
            }
            bool success = StartService(serviceHandle, 0, null);
            if(success == false)
            {
                int error = Marshal.GetLastWin32Error();
                CloseServiceHandle(serviceHandle);
                CloseServiceHandle(scmHandle);
                throw new Win32Exception(error);
            }

            CloseServiceHandle(serviceHandle);
            
            CloseServiceHandle(scmHandle);
            return success;
        }


        public bool ServiceExists(string serviceName)
        {
            // Check if the service exists
            ServiceController[] services = ServiceController.GetServices();
            return services.Any(service => service.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        }

        public bool IsServiceRunning(string serviceName)
        {
            using (ServiceController serviceController = new ServiceController(serviceName))
            {
                return serviceController.Status == ServiceControllerStatus.Running;
            }
        }

        public bool StopService(string serviceName)
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

        public string ReadPasswordFromConsole()
        {
            StringBuilder passwordBuilder = new StringBuilder();

                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                    else if (key.Key == ConsoleKey.Backspace && passwordBuilder.Length > 0)
                    {
                        passwordBuilder.Length -= 1;
                        Console.Write("\b \b");
                    }
                    else
                    {
                        passwordBuilder.Append(key.KeyChar);
                        Console.Write("*");
                    }
                }
                Console.WriteLine();
                return passwordBuilder.ToString();
        }
    }
}