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
}
