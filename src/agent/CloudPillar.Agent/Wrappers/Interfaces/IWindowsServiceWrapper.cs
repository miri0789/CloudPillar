
namespace CloudPillar.Agent.Wrappers
{
    public interface IWindowsServiceWrapper
    {
        void InstallWindowsService(string serviceName, string workingDirectory, string serviceDescription);
    }
}