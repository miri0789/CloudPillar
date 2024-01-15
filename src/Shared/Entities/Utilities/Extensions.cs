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
        switch (changeSpecKey)
        {

            case TwinPatchChangeSpec.Diagnostics:
                return twinReported.ChangeSpec[1];
            case TwinPatchChangeSpec.ServerIdentity:
                return twinReported.ChangeSpec[2];
            case TwinPatchChangeSpec.ChangeSpec:
            default:
                return twinReported.ChangeSpec[0];
        }
    }
    public static TwinChangeSpec? GetDesiredChangeSpecByKey(this TwinDesired twinDesired, TwinPatchChangeSpec changeSpecKey)
    {
        switch (changeSpecKey)
        {
            case TwinPatchChangeSpec.Diagnostics:
                return twinDesired.ChangeSpec[1];
            case TwinPatchChangeSpec.ServerIdentity:
                return twinDesired.ChangeSpec[2];
            case TwinPatchChangeSpec.ChangeSpec:
            default:
                return twinDesired.ChangeSpec[0];
        }
    }

    public static void SetReportedChangeSpecByKey(this TwinReported twinReported, TwinReportedChangeSpec twinReportedChangeSpec, TwinPatchChangeSpec changeSpecKey)
    {
        switch (changeSpecKey)
        {
            case TwinPatchChangeSpec.ChangeSpec:
                twinReported.ChangeSpec[0] = twinReportedChangeSpec; break;
            case TwinPatchChangeSpec.Diagnostics:
                twinReported.ChangeSpec[1] = twinReportedChangeSpec; break;
            case TwinPatchChangeSpec.ServerIdentity:
                twinReported.ChangeSpec[2] = twinReportedChangeSpec; break;

        }
    }


}
