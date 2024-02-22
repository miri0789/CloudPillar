using CloudPillar.Agent.Entities;

namespace CloudPillar.Agent.Utilities.Interfaces
{
    public interface ICheckExceptionResult
    {
        DeviceConnectResultEnum? IsDeviceConnectException(string message);
    }
}

