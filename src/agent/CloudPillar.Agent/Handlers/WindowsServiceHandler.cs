using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers.Logger;
using System.ComponentModel;
using System.Runtime.InteropServices;
using CloudPillar.Agent.Utilities;

namespace CloudPillar.Agent.Handlers
{
    public class WindowsServiceHandler : IWindowsServiceHandler
    {
        private readonly IWindowsServiceUtils _windowsServiceUtils;
        private readonly ILoggerHandler _logger;

        public WindowsServiceHandler(IWindowsServiceUtils windowsServiceUtils, ILoggerHandler logger)
        {
            _windowsServiceUtils = windowsServiceUtils ?? throw new ArgumentNullException(nameof(windowsServiceUtils));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void InstallWindowsService(string serviceName, string workingDirectory, string serviceDescription, string? userPassword)
        {
            try
                {
                if (_windowsServiceUtils.ServiceExists(serviceName))
                {
                    if (_windowsServiceUtils.IsServiceRunning(serviceName))
                    {
                        if (_windowsServiceUtils.StopService(serviceName))
                        {
                            _logger.Info("Service stopped successfully.");
                        }
                        else
                        {
                            _logger.Error("Failed to stop service.");
                        }
                    }
                    // delete existing service
                    if (_windowsServiceUtils.DeleteExistingService(serviceName))
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
                if (_windowsServiceUtils.CreateAndStartService(serviceName, workingDirectory, serviceDescription, userPassword))
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
    }
}