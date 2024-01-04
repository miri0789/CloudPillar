using CloudPillar.Agent.Entities;
using Microsoft.Azure.Devices.Client.Transport;

namespace CloudPillar.Agent.Handlers;

public interface IWindowsServiceHandler
{
        void InstallWindowsService(string serviceName, string workingDirectory, string serviceDescription);
}