using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Shared.Entities.Twin;

namespace Shared.Entities.Utilities;

public static class JsonConvertExtensions
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

    public static TwinReportedChangeSpec GetReportedChangeSpecByKey(this TwinReported twinReported, TwinPatchChangeSpec changeSpecKey)
    {
        switch (changeSpecKey)
        {

            case TwinPatchChangeSpec.changeSpecDiagnostics:
                return twinReported.ChangeSpecDiagnostics;
            case TwinPatchChangeSpec.ChangeSpec:
            default:
                return twinReported.ChangeSpec;
        }
    }
    public static TwinChangeSpec GetDesiredChangeSpecByKey(this TwinDesired twinDesired, TwinPatchChangeSpec changeSpecKey)
    {
        switch (changeSpecKey)
        {
            case TwinPatchChangeSpec.changeSpecDiagnostics:
                return twinDesired.ChangeSpecDiagnostics;
            case TwinPatchChangeSpec.ChangeSpec:
            default:
                return twinDesired.ChangeSpec;
        }
    }


}
