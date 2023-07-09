using Microsoft.Azure.Devices.Client;

namespace CloudPillar.Agent.Interfaces;
public interface ICommonHandler
{
    string GetDeviceIdFromConnectionString(string connectionString);
    TransportType GetTransportType();
}