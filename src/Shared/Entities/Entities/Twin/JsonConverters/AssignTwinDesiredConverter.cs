using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shared.Entities.Twin;

public class AssignTwinDesiredConverter : JsonConverter
{

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(AssignChangeSpec);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);

        var ChangeSpecKey = (jsonObject["ChangeSpecKey"] ?? jsonObject["changeSpecKey"])?.Value<string>();
        var changeSpec = new AssignChangeSpec()
        {
            ChangeSpecKey = ChangeSpecKey,
            Devices = (jsonObject["Devices"] ?? jsonObject["devices"])?.Value<string>(),
            // ChangeSpec = GetChangeSpec(jsonObject, serializer, ChangeSpecKey)
        };
        return changeSpec;
    }

    private AssignTwinChangeSpec? GetChangeSpec(JObject jsonObject, JsonSerializer serializer, string ChangeSpecKey)
    {
        var changeSpec = new AssignTwinChangeSpec();

        if (jsonObject.Type == JTokenType.Object &&
            (jsonObject["ChangeSpec"] ?? jsonObject["changeSpec"]) != null)
        {
            changeSpec = CreateTwinChangeSpec(jsonObject, serializer, "changeSpec");
        }

        return changeSpec;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new Exception();
    }

    private AssignTwinChangeSpec CreateTwinChangeSpec(JObject jsonObject, JsonSerializer serializer, string changeSpecKey)
    {
        var lowerPropName = FirstLetterToLowerCase($"{changeSpecKey}");
        var upperPropName = FirstLetterToUpperCase($"{changeSpecKey}");
        var r = (jsonObject.SelectToken($"{lowerPropName}") ?? jsonObject.SelectToken($"{upperPropName}"))?.Value<JProperty>();

        var changeSpec = new AssignTwinChangeSpec()
        {
            Id = (jsonObject.SelectToken($"{lowerPropName}.id") ?? jsonObject.SelectToken($"{upperPropName}.Id"))?.Value<string>(),
            // Patch = GetDynamicPatch(jsonObject, lowerPropName, upperPropName, serializer)
        };
        return changeSpec;
    }

    private Dictionary<string, TwinAction[]> GetDynamicPatch(JObject jsonObject, string lowerPropName, string upperPropName, JsonSerializer serializer)
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