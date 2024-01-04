
namespace CloudPillar.Agent.Utilities
{
    public interface IWindowsServiceUtils
    {
        bool StopService(string serviceName);
        bool ServiceExists(string serviceName);
        bool DeleteExistingService(string serviceName);
        bool CreateAndStartService(string serviceName, string workingDirectory, string serviceDescription, string? userPassword);
        string ReadPasswordFromConsole();
        bool IsServiceRunning(string serviceName);

    }
}