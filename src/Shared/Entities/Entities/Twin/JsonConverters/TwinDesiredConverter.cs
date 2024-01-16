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
            // ChangeSpecDiagnostics = CreateTwinChangeSpec(jsonObject, serializer, TwinPatchChangeSpec.Diagnostics)
            // ChangeSpecList = GetTwinChangeSpecList(jsonObject, serializer)
        };
        return changeSpec;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    private List<TwinChangeSpec> GetTwinChangeSpecList(JObject jsonObject, JsonSerializer serializer)
    {
        var changeSpecList = new List<TwinChangeSpec>();

        var changeSpecToken = jsonObject.SelectToken(FirstLetterToLowerCase($"changeSpec"))
        ?? jsonObject.SelectToken(FirstLetterToUpperCase($"changeSpec"));

        if (changeSpecToken is JArray changeSpecArray)
        {
            foreach (var changeSpecObject in changeSpecArray)
            {
                var changeSpecKey = changeSpecObject[FirstLetterToLowerCase("id")]?.Value<string>() ??
                changeSpecObject[FirstLetterToUpperCase("id")]?.Value<string>();
                var changeSpecKeyPrefix = changeSpecKey.Split('-').FirstOrDefault();

                if (Enum.TryParse<TwinPatchChangeSpec>(changeSpecKeyPrefix, out var enumValue))
                {
                    var twinChangeSpec = CreateTwinChangeSpec(changeSpecObject, serializer, enumValue);
                    changeSpecList.Add(twinChangeSpec);
                }
            }
        }

        return changeSpecList;
    }
    private TwinChangeSpec CreateTwinChangeSpec(JToken jsonObject, JsonSerializer serializer, TwinPatchChangeSpec changeSpecKey)
    {
        var lowerPropName = FirstLetterToLowerCase($"{changeSpecKey}");
        var upperPropName = FirstLetterToUpperCase($"{changeSpecKey}");
        var changeSpec = new TwinChangeSpec()
        {
            Id = (jsonObject.SelectToken($"{lowerPropName}.id") ?? jsonObject.SelectToken($"{upperPropName}.Id"))?.Value<string>(),
            Patch = GetDynamicPatch(jsonObject, lowerPropName, upperPropName, serializer)
        };
        return changeSpec;
    }


    private Dictionary<string, TwinAction[]> GetDynamicPatch(JToken jsonObject, string lowerPropName, string upperPropName, JsonSerializer serializer)
    {
        var dynamicPatch = new Dictionary<string, TwinAction[]>();

        var patchToken = jsonObject.SelectToken($"{lowerPropName}.patch") ?? jsonObject.SelectToken($"{upperPropName}.Patch");

        if (patchToken is JObject patchObject)
        {
            foreach (var property in patchObject.Properties())
            {
                dynamicPatch.Add(property.Name, property.Value.ToObject<TwinAction[]>(serializer));
            }
        }

        return dynamicPatch;
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