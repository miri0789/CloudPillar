
namespace CloudPillar.Agent.Utilities
{
    public interface IWindowsServiceUtils
    {
        bool StopService(string serviceName);
        bool ServiceExists(string serviceName);
        bool DeleteExistingService(string serviceName);
        void CreateService(string serviceName, string workingDirectory, string serviceDescription, string? userPassword);
        bool StartService(string serviceName);
        string ReadPasswordFromConsole();
        bool IsServiceRunning(string serviceName);

    }
}