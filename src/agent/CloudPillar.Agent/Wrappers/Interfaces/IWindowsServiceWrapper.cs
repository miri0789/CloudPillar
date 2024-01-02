
namespace CloudPillar.Agent.Wrappers
{
    public interface IWindowsServiceWrapper
    {
        bool StopService(string serviceName);
        bool ServiceExists(string serviceName);
        bool DeleteExistingService(string serviceName);
        bool CreateAndStartService(string serviceName, string workingDirectory);
        string ReadPasswordFromConsole();
        bool IsServiceRunning(string serviceName);

    }
}