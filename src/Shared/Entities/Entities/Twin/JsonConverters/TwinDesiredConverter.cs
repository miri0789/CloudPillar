using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shared.Entities.Twin;

public class TwinDesiredConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TwinDesired);
    }
    string GetCasedPropertyName(string propName) =>
        $"{FirstLetterToLowerCase(propName)}" ?? $"{FirstLetterToUpperCase(propName)}";

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {

        JObject jsonObject = JObject.Load(reader);
        var changeSpec = new TwinDesired()
        {
            ChangeSign = jsonObject[GetCasedPropertyName("changeSign")]?.Value<string>(),
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
        var changeSpec = new TwinChangeSpec()
        {
            Id = jsonObject.SelectToken($"{GetCasedPropertyName($"{changeSpecKey}.id")}")?.Value<string>(),
            Patch = new TwinPatch
            {
                PreTransitConfig = jsonObject.SelectToken($"{GetCasedPropertyName($"{changeSpecKey}.patch.preTransitConfig")}")?.ToObject<TwinAction[]>(serializer),
                TransitPackage = jsonObject.SelectToken($"{GetCasedPropertyName($"{changeSpecKey}.patch.transitPackage")}")?.ToObject<TwinAction[]>(serializer),
                PreInstallConfig = jsonObject.SelectToken($"{GetCasedPropertyName($"{changeSpecKey}.patch.preInstallConfig")}")?.ToObject<TwinAction[]>(serializer),
                InstallSteps = jsonObject.SelectToken($"{GetCasedPropertyName($"{changeSpecKey}.patch.installSteps")}")?.ToObject<TwinAction[]>(serializer),
                PostInstallConfig = jsonObject.SelectToken($"{GetCasedPropertyName($"{changeSpecKey}.patch.postInstallConfig")}")?.ToObject<TwinAction[]>(serializer),
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