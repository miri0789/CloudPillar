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
        var lowerPropName = FirstLetterToLowerCase(TwinPatchChangeSpec.ChangeSpec.ToString());
        var upperPropName = FirstLetterToUpperCase(TwinPatchChangeSpec.ChangeSpec.ToString());

        var lowerDiagnosticsPropName = FirstLetterToLowerCase(TwinPatchChangeSpec.Diagnostics.ToString());
        var upperDiagnosticsPropName = FirstLetterToUpperCase(TwinPatchChangeSpec.Diagnostics.ToString());

        var changeSpec = new TwinDesired()
        {
            ChangeSign = (jsonObject["changeSign"] ?? jsonObject["ChangeSign"])?.Value<string>(),

            ChangeSpec = GetTwinChangeSpecList(jsonObject, serializer)
        };
        return changeSpec;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    private TwinChangeSpec CreateTwinChangeSpec(JObject jsonObject, JsonSerializer serializer)
    {
        var changeSpec = new TwinChangeSpec()
        {
            Id = (jsonObject.SelectToken($"id") ?? jsonObject.SelectToken($"Id"))?.Value<string>(),
            Patch = GetDynamicPatch(jsonObject, serializer)
        };
        return changeSpec;
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
                var twinChangeSpec = CreateTwinChangeSpec((JObject)changeSpecObject, serializer);
                changeSpecList.Add(twinChangeSpec);

            }
        }

        return changeSpecList;
    }  
    private Dictionary<string, TwinAction[]> GetDynamicPatch(JObject jsonObject, JsonSerializer serializer)
    {
        var dynamicPatch = new Dictionary<string, TwinAction[]>();

        var patchToken = jsonObject.SelectToken($"patch") ?? jsonObject.SelectToken($"Patch");

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