using Newtonsoft.Json;
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
           Converters = new List<JsonConverter> {
                                        new TwinDesiredConverter() },
           Formatting = Formatting.Indented,
           NullValueHandling = NullValueHandling.Ignore
       }));
        return twinDesiredJson;
    }

    public static JObject ConvertToJObject(this TwinReported twinReported)
    {
        var twinReportedJson = JObject.Parse(JsonConvert.SerializeObject(twinReported,
       Formatting.None,
       new JsonSerializerSettings
       {
           ContractResolver = new CamelCasePropertyNamesContractResolver(),
           Converters = new List<JsonConverter> {
                                        new TwinReportedConverter() },
           Formatting = Formatting.Indented,
           NullValueHandling = NullValueHandling.Ignore
       }));
        return twinReportedJson;
    }


    public static TwinReportedChangeSpec? GetReportedChangeSpecByKey(this TwinReported twinReported, string changeSpecKey)
    {
        return twinReported?.ChangeSpec?.FirstOrDefault(x => x.Key.ToLower() == changeSpecKey.ToLower()).Value;
    }

    public static TwinChangeSpec? GetDesiredChangeSpecByKey(this TwinDesired twinDesired, string changeSpecKey)
    {
        return twinDesired?.ChangeSpec?.FirstOrDefault(x => x.Key.ToLower() == changeSpecKey.ToLower()).Value;
    }

    public static void SetReportedChangeSpecByKey(this TwinReported twinReported, TwinReportedChangeSpec twinReportedChangeSpec, string changeSpecKey)
    {
        if (twinReported is not null && twinReported.ChangeSpec is not null)
        {
            if (twinReported?.ChangeSpec.FirstOrDefault(x => x.Key.ToLower() == changeSpecKey.ToLower()).Key is not null)
            {
                twinReported.ChangeSpec[changeSpecKey] = twinReportedChangeSpec;
                return;
            }
            else
            {
                twinReported.ChangeSpec.Add(changeSpecKey, twinReportedChangeSpec);
                return;
            }
        }
    }

    public static string? GetReportedChangeSignByKey(this TwinReported twinReported, string changeSignKey)
    {
        return twinReported?.ChangeSign?.FirstOrDefault(x => x.Key.ToLower() == changeSignKey.ToLower()).Value;
    }

    public static string? GetDesiredChangeSignByKey(this TwinDesired twinDesired, string changeSignKey)
    {
        return twinDesired?.ChangeSign?.FirstOrDefault(x => x.Key.ToLower() == changeSignKey.ToLower()).Value;
    }

    public static string GetSignKeyByChangeSpec(this string changeSpecKey)
    {
        return $"{changeSpecKey}{DeviceConstants.SIGN_KEY}";
    }

    public static string GetSpecKeyBySignKey(this string changeSignKey)
    {
        return changeSignKey.Replace(DeviceConstants.SIGN_KEY, "");
    }
    
    public static void SetDesiredChangeSignByKey(this TwinDesired twinDesired, string changeSignKey, string signData)
    {
        if (twinDesired is not null && twinDesired.ChangeSign is not null)
        {
            if (twinDesired?.ChangeSign.FirstOrDefault(x => x.Key.ToLower() == changeSignKey.ToLower()).Key is not null)
            {
                twinDesired.ChangeSign[changeSignKey] = signData;
                return;
            }
            else
            {
                twinDesired.ChangeSign.Add(changeSignKey, signData);
                return;
            }
        }
    }
}
