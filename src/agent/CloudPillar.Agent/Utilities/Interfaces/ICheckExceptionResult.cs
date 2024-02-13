using CloudPillar.Agent.Enums;

namespace CloudPillar.Agent.Utilities.Interfaces
{
    public interface ICheckExceptionResult
    {
        DeviceConnectionResult? IsDeviceConnectException(Exception ex);
    }
}

