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
                return twinReported.ChangeSpecList[1];
            case TwinPatchChangeSpec.ServerIdentity:
                return twinReported.ChangeSpecList[2];
            case TwinPatchChangeSpec.ChangeSpec:
            default:
                return twinReported.ChangeSpecList[0];
        }
    }
    public static TwinChangeSpec? GetDesiredChangeSpecByKey(this TwinDesired twinDesired, TwinPatchChangeSpec changeSpecKey)
    {
        switch (changeSpecKey)
        {
            case TwinPatchChangeSpec.Diagnostics:
                return twinDesired.ChangeSpecList[1];
            case TwinPatchChangeSpec.ServerIdentity:
                return twinDesired.ChangeSpecList[2];
            case TwinPatchChangeSpec.ChangeSpec:
            default:
                return twinDesired.ChangeSpecList[0];
        }
    }

    public static void SetReportedChangeSpecByKey(this TwinReported twinReported, TwinReportedChangeSpec twinReportedChangeSpec, TwinPatchChangeSpec changeSpecKey)
    {
        switch (changeSpecKey)
        {
            case TwinPatchChangeSpec.ChangeSpec:
                twinReported.ChangeSpecList[0] = twinReportedChangeSpec; break;
            case TwinPatchChangeSpec.Diagnostics:
                twinReported.ChangeSpecList[1] = twinReportedChangeSpec; break;
            case TwinPatchChangeSpec.ServerIdentity:
                twinReported.ChangeSpecList[2] = twinReportedChangeSpec; break;

        }
    }


}
