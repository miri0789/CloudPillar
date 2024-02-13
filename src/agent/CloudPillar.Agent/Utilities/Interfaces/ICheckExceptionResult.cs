using CloudPillar.Agent.Enums;

namespace CloudPillar.Agent.Utilities.Interfaces
{
    public interface ICheckExceptionResult
    {
        DeviceConnectResultEnum? IsDeviceConnectException(Exception ex);
    }
}

