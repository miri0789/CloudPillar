using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.Entities.Twin;

namespace Shared.Entities.Utilities;

public static class TwinJsonConvertExtensions
{
    public static TwinDesired ConvertToTwinDesired(this string json)
    {
        var twinDesired = JsonConvert.DeserializeObject<TwinDesired>(json,
            new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> {
                                        new TwinDesiredConverter(), new TwinActionConverter() }
            });
        return twinDesired;

    }

     public static TwinReported ConvertToTwinReported(this string json)
    {
        var twinReported = JsonConvert.DeserializeObject<TwinReported>(json,
            new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> {
                                        new TwinReportedConverter(), new TwinActionConverter() }
            });
        return twinReported;

    }
    
    public static JObject ConvertToJObject(this TwinDesired twinDesired)
    {
        var twinDesiredJson = JObject.Parse(JsonConvert.SerializeObject(twinDesired,
       Formatting.None,
       new JsonSerializerSettings
       {
           ContractResolver = new CamelCasePropertyNamesContractResolver(),
           Converters = { new StringEnumConverter() },
           Formatting = Formatting.Indented,
           NullValueHandling = NullValueHandling.Ignore
       }));
        return twinDesiredJson;

    }

    public static TwinReportedChangeSpec? GetReportedChangeSpecByKey(this TwinReported twinReported, string changeSpecKey)
    {
        return twinReported?.ChangeSpec?.FirstOrDefault(x => x.Key.ToLower().Contains(changeSpecKey?.ToLower())).Value;
    }
   
    public static TwinChangeSpec? GetDesiredChangeSpecByKey(this TwinDesired twinDesired, string changeSpecKey)
    {
        return twinDesired?.ChangeSpec?.FirstOrDefault(x => x.Key.ToLower().Contains(changeSpecKey.ToString().ToLower())).Value;
    }
   
    public static void SetReportedChangeSpecByKey(this TwinReported twinReported, TwinReportedChangeSpec twinReportedChangeSpec, string changeSpecKey)
    {

        var matchingChangeSpecKey = twinReported?.ChangeSpec?.FirstOrDefault(x => x.Key.ToLower().Contains(changeSpecKey.ToString().ToLower())).Key;

        if (matchingChangeSpecKey != null)
        {
            twinReported.ChangeSpec[matchingChangeSpecKey] = twinReportedChangeSpec;
        }
    }

    public static string? GetReportedChangeSignByKey(this TwinReported twinReported, string changeSpecKey)
    {
        return twinReported?.ChangeSign?.FirstOrDefault(x => x.Key.ToLower().Contains(changeSpecKey.ToString().ToLower())).Value;
    }
   
    public static string? GetDesiredChangeSignByKey(this TwinDesired twinDesired, string changeSpecKey)
    {
        return twinDesired?.ChangeSign?.FirstOrDefault(x => x.Key.ToLower().Contains(changeSpecKey.ToString().ToLower())).Value;
    }
   

}
