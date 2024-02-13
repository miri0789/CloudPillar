using CloudPillar.Agent.Enums;
using CloudPillar.Agent.Utilities.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CloudPillar.Agent.Utilities;

public class CheckExceptionResult : ICheckExceptionResult
{
    public DeviceConnectResultEnum? IsDeviceConnectException(Exception ex)
    {
        try
        {
            var exceptionData = JObject.Parse(ex.Message);
            var error = exceptionData?["errorCode"]?.ToString();
            return error is not null && Enum.TryParse(error, out DeviceConnectResultEnum errorCode) && errorCode == DeviceConnectResultEnum.IotHubUnauthorizedAccess ? errorCode : null;
        }
        catch (JsonReaderException exception)
        {
            return null;
        }
    }
}