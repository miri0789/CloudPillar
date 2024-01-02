using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers.Logger;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CloudPillar.Agent.Handlers
{
    public class WindowsServiceHandler : IWindowsServiceHandler
    {
        private readonly IWindowsServiceWrapper _windowsServiceWrapper;
        private readonly ILoggerHandler _logger;

        public WindowsServiceHandler(IWindowsServiceWrapper windowsServiceWrapper, ILoggerHandler logger)
        {
            _windowsServiceWrapper = windowsServiceWrapper ?? throw new ArgumentNullException(nameof(windowsServiceWrapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void InstallWindowsService(string serviceName, string workingDirectory)
        {
            try
                {
                if (_windowsServiceWrapper.ServiceExists(serviceName))
                {
                    if (_windowsServiceWrapper.IsServiceRunning(serviceName))
                    {
                        if (_windowsServiceWrapper.StopService(serviceName))
                        {
                            _logger.Info("Service stopped successfully.");
                        }
                        else
                        {
                            _logger.Error("Failed to stop service.");
                        }
                    }
                    // delete existing service
                    if (_windowsServiceWrapper.DeleteExistingService(serviceName))
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
                if (_windowsServiceWrapper.CreateAndStartService(serviceName, workingDirectory))
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