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

    public static TwinReportedChangeSpec? GetReportedChangeSpecByKey(this TwinReported twinReported, TwinPatchChangeSpec changeSpecKey)
    {
        if (changeSpecKey == TwinPatchChangeSpec.ChangeSpec)
        {
            return twinReported?.ChangeSpec?.FirstOrDefault();
        }
        return twinReported?.ChangeSpec?.FirstOrDefault(x => x.Id.ToLower().Contains(changeSpecKey.ToString().ToLower()));
    }
    public static TwinChangeSpec? GetDesiredChangeSpecByKey(this TwinDesired twinDesired, TwinPatchChangeSpec changeSpecKey)
    {
        if (changeSpecKey == TwinPatchChangeSpec.ChangeSpec)
        {
            return twinDesired?.ChangeSpec?.FirstOrDefault();
        }

        return twinDesired?.ChangeSpec?.FirstOrDefault(x => x.Id.ToLower().Contains(changeSpecKey.ToString().ToLower()));
    }
    public static void SetReportedChangeSpecByKey(this TwinReported twinReported, TwinReportedChangeSpec twinReportedChangeSpec, TwinPatchChangeSpec changeSpecKey)
    {
        twinReported.ChangeSpec ??= new List<TwinReportedChangeSpec>();
        var changeSpec = twinReported?.ChangeSpec.FirstOrDefault(x => x.Id.ToLower().Contains(changeSpecKey.ToString().ToLower()));
        if (changeSpecKey == TwinPatchChangeSpec.ChangeSpec)
        {
            changeSpec = twinReported?.ChangeSpec?.FirstOrDefault();
        }
        if (changeSpec is null)
        {
            twinReported.ChangeSpec.Add(twinReportedChangeSpec);
        }
        else
        {
            changeSpec = twinReportedChangeSpec;
        }
    }
}
