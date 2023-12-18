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
            ChangeSpec = CreateTwinChangeSpec(jsonObject, serializer, TwinPatchChangeSpec.ChangeSpec),
            ChangeSpecDiagnostics = CreateTwinChangeSpec(jsonObject, serializer, TwinPatchChangeSpec.ChangeSpecDiagnostics)
        };
        return changeSpec;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    private TwinChangeSpec CreateTwinChangeSpec(JObject jsonObject, JsonSerializer serializer, TwinPatchChangeSpec changeSpecKey)
    {
        var lowerPropName = FirstLetterToLowerCase($"{changeSpecKey}");
        var upperPropName = FirstLetterToUpperCase($"{changeSpecKey}");
        var changeSpec = new TwinChangeSpec()
        {
            Id = (jsonObject.SelectToken($"{lowerPropName}.id") ?? jsonObject.SelectToken($"{upperPropName}.Id"))?.Value<string>(),
            Patch = new TwinPatch
            {
                PreTransitConfig = (jsonObject.SelectToken($"{lowerPropName}.patch.preTransitConfig") ?? jsonObject.SelectToken($"{upperPropName}.Patch.PreTransitConfig"))?.ToObject<TwinAction[]>(serializer),
                TransitPackage = (jsonObject.SelectToken($"{lowerPropName}.patch.transitPackage") ?? jsonObject.SelectToken($"{upperPropName}.Patch.TransitPackage"))?.ToObject<TwinAction[]>(serializer),
                PreInstallConfig = (jsonObject.SelectToken($"{lowerPropName}.patch.preInstallConfig") ?? jsonObject.SelectToken($"{upperPropName}.Patch.PreInstallConfig"))?.ToObject<TwinAction[]>(serializer),
                InstallSteps = (jsonObject.SelectToken($"{lowerPropName}.patch.installSteps") ?? jsonObject.SelectToken($"{upperPropName}.Patch.InstallSteps"))?.ToObject<TwinAction[]>(serializer),
                PostInstallConfig = (jsonObject.SelectToken($"{lowerPropName}.patch.postInstallConfig") ?? jsonObject.SelectToken($"{upperPropName}.Patch.PostInstallConfig"))?.ToObject<TwinAction[]>(serializer),
            }
        };
        return changeSpec;
    }
    private string FirstLetterToLowerCase(string input)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(input);
        return string.Join(".", input.Split('.').Select(s => char.ToLower(s.FirstOrDefault()) + s.Substring(1)));
    }

    private string FirstLetterToUpperCase(string input)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(input);
        return string.Join(".", input.Split('.').Select(s => char.ToUpper(s.FirstOrDefault()) + s.Substring(1)));
    }

}