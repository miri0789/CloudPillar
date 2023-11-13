
using System.Reflection.Metadata.Ecma335;
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
            ChangeSpec = CretaeTwinChangeSpec(jsonObject, serializer, TwinPatchChangeSpec.ChangeSpec.ToString()),
            ChangeSpecDiagnostics = CretaeTwinChangeSpec(jsonObject, serializer, TwinPatchChangeSpec.changeSpecDiagnostics.ToString())
        };
        return changeSpec;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    private TwinChangeSpec CretaeTwinChangeSpec(JObject jsonObject, JsonSerializer serializer, string propName)
    {
        var changeSpec = new TwinChangeSpec()
        {
            Id = (jsonObject.SelectToken($"{FirstLetterToLowerCase(propName)}.id") ?? jsonObject.SelectToken($"{FirstLetterToUpperCase(propName)}.Id"))?.Value<string>(),
            Patch = new TwinPatch()
            {
                PreTransitConfig = (jsonObject.SelectToken($"{FirstLetterToLowerCase(propName)}.patch.preTransitConfig") ?? jsonObject.SelectToken($"{FirstLetterToUpperCase(propName)}.Patch.PreTransitConfig"))?.ToObject<TwinAction[]>(serializer),
                TransitPackage = (jsonObject.SelectToken($"{FirstLetterToLowerCase(propName)}.patch.transitPackage") ?? jsonObject.SelectToken($"{FirstLetterToUpperCase(propName)}.Patch.TransitPackage"))?.ToObject<TwinAction[]>(serializer),
                PreInstallConfig = (jsonObject.SelectToken($"{FirstLetterToLowerCase(propName)}.patch.preInstallConfig") ?? jsonObject.SelectToken($"{FirstLetterToUpperCase(propName)}.Patch.PreInstallConfig"))?.ToObject<TwinAction[]>(serializer),
                InstallSteps = (jsonObject.SelectToken($"{FirstLetterToLowerCase(propName)}.patch.installSteps") ?? jsonObject.SelectToken($"{FirstLetterToUpperCase(propName)}.Patch.InstallSteps"))?.ToObject<TwinAction[]>(serializer),
                PostInstallConfig = (jsonObject.SelectToken($"{FirstLetterToLowerCase(propName)}.patch.postInstallConfig") ?? jsonObject.SelectToken($"{FirstLetterToUpperCase(propName)}.Patch.PostInstallConfig"))?.ToObject<TwinAction[]>(serializer),
            }
        };
        return changeSpec;
    }
    private string FirstLetterToLowerCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }
        char[] inputArray = input.ToCharArray();
        inputArray[0] = char.ToLower(inputArray[0]);
        return new string(inputArray);
    }

    private string FirstLetterToUpperCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }
        char[] inputArray = input.ToCharArray();
        inputArray[0] = char.ToUpper(inputArray[0]);
        return new string(inputArray);
    }


}