using CloudPillar.Agent.Entities;
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
            var exceptionData = !string.IsNullOrEmpty(ex.Message) ? JObject.Parse(ex.Message) : null;
            var error = exceptionData?["errorCode"]?.ToString();
            return error is not null && Enum.TryParse(error, out DeviceConnectResultEnum errorCode) ? errorCode : null;
        }
        catch (Exception exception)
        {
            return null;
        }
    }
}