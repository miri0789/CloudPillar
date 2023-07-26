
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shared.Entities.Twin;

public class TwinDesiredConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TwinDesired);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        var changeSpec = new TwinDesired()
        {
            ChangeSign = jsonObject["changeSign"]?.Value<string>(),
            ChangeSpec = new TwinChangeSpec()
            {
                Id = jsonObject.SelectToken("changeSpec.id")?.Value<string>(),
                Patch = new TwinPatch()
                {
                    PreTransitConfig = jsonObject.SelectToken("changeSpec.patch.preTransitConfig")?.ToObject<TwinAction[]>(serializer),
                    TransitPackage = jsonObject.SelectToken("changeSpec.patch.transitPackage")?.ToObject<TwinAction[]>(serializer),
                    PreInstallConfig = jsonObject.SelectToken("changeSpec.patch.preInstallConfig")?.ToObject<TwinAction[]>(serializer),
                    InstallSteps = jsonObject.SelectToken("changeSpec.patch.installSteps")?.ToObject<TwinAction[]>(serializer),
                    PostInstallConfig = jsonObject.SelectToken("changeSpec.patch.postInstallConfig")?.ToObject<TwinAction[]>(serializer)
                }
            }
        };
        return changeSpec;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}