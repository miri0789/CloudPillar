
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
            ChangeSign = (jsonObject["changeSign"] ?? jsonObject["ChangeSign"])?.Value<string>(),
            ChangeSpec = new TwinChangeSpec()
            {
                Id = (jsonObject.SelectToken("changeSpec.id") ?? jsonObject.SelectToken("ChangeSpec.Id"))?.Value<string>(),
                Patch = new TwinPatch()
                {
                    PreTransitConfig = (jsonObject.SelectToken("changeSpec.patch.preTransitConfig") ?? jsonObject.SelectToken("ChangeSpec.Patch.PreTransitConfig"))?.ToObject<TwinAction[]>(serializer),
                    TransitPackage = (jsonObject.SelectToken("changeSpec.patch.transitPackage") ?? jsonObject.SelectToken("ChangeSpec.Patch.TransitPackage"))?.ToObject<TwinAction[]>(serializer),
                    PreInstallConfig = (jsonObject.SelectToken("changeSpec.patch.preInstallConfig") ?? jsonObject.SelectToken("ChangeSpec.Patch.PreInstallConfig"))?.ToObject<TwinAction[]>(serializer),
                    InstallSteps = (jsonObject.SelectToken("changeSpec.patch.installSteps") ?? jsonObject.SelectToken("ChangeSpec.Patch.InstallSteps"))?.ToObject<TwinAction[]>(serializer),
                    PostInstallConfig = (jsonObject.SelectToken("changeSpec.patch.postInstallConfig") ?? jsonObject.SelectToken("ChangeSpec.Patch.PostInstallConfig"))?.ToObject<TwinAction[]>(serializer),
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