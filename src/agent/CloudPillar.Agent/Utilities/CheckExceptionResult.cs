using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Utilities.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CloudPillar.Agent.Utilities;

public class CheckExceptionResult : ICheckExceptionResult
{
    public DeviceConnectResultEnum? IsDeviceConnectException(string message)
    {
        try
        {
            if (string.IsNullOrEmpty(message))
            {
                return null;
            }
            var isValidJson = IsValidJson(message);
            if (!isValidJson)
            {
                return IsDeviceConnectException(GetJsonMsgFromMsg(message));
            }
            var exceptionData = JObject.Parse(message);
            var error = exceptionData?["errorCode"]?.ToString();
            return error is not null && Enum.TryParse(error, out DeviceConnectResultEnum errorCode) ? errorCode : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private string GetJsonMsgFromMsg(string errorMessage)
    {
        var startIndex = errorMessage.IndexOf('{');
        var endIndex = errorMessage.LastIndexOf('}');
        if (startIndex != -1 && endIndex != -1)
        {
            return errorMessage.Substring(startIndex, endIndex - startIndex + 1);
        }
        return string.Empty;
    }

    private bool IsValidJson(string jsonString)
    {
        try
        {
            var jsonObject = JsonConvert.DeserializeObject(jsonString);
            return true;
        }
        catch (JsonReaderException)
        {
            return false;
        }
    }
}