using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shared.Entities.Twin;

public class TwinReportedConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TwinReported);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        var changeSpec = new TwinReported()
        {
            ChangeSign = GetChangeSign(jsonObject, serializer),
            ChangeSpec = GetChangeSpec(jsonObject, serializer),
            // DeviceState = GetValueOrDefault<DeviceStateType>(jsonObject, "deviceState") ?? GetValueOrDefault<DeviceStateType>(jsonObject, "DeviceState"),
            // DeviceState = (jsonObject["deviceState"] ?? jsonObject["DeviceState"])?.Value<DeviceStateType>(),
            // AgentPlatform = (jsonObject["agentPlatform"] ?? jsonObject["AgentPlatform"])?.Value<string>(),
            // SupportedShells = (jsonObject["supportedShells"] ?? jsonObject["SupportedShells"])?.Value<ShellType[]>(),
            // SecretKey = (jsonObject["secretKey"] ?? jsonObject["SecretKey"])?.Value<string>(),
            // Custom = (jsonObject["custom"] ?? jsonObject["Custom"])?.Value<List<TwinReportedCustomProp>>(),
            ChangeSpecId = (jsonObject["changeSpecId"] ?? jsonObject["ChangeSpecId"])?.Value<string>(),
            CertificateValidity = (jsonObject["certificateValidity"] ?? jsonObject["CertificateValidity"])?.Value<CertificateValidity>(),
            DeviceStateAfterServiceRestart = (jsonObject["deviceStateAfterServiceRestart"] ?? jsonObject["DeviceStateAfterServiceRestart"])?.Value<DeviceStateType>(),
            KnownIdentities = (jsonObject["knownIdentities"] ?? jsonObject["KnownIdentities"])?.Value<List<KnownIdentities>>(),
        };
        return changeSpec;
    }
    private T GetValueOrDefault<T>(JObject jsonObject, string propertyName)
    {
        JToken token = jsonObject[propertyName];
        return token != null ? token.Value<T>() : default(T);
    }
    private IDictionary<string, string>? GetChangeSign(JObject jsonObject, JsonSerializer serializer)
    {
        var changeSign = new Dictionary<string, string>();

        foreach (var property in jsonObject.Properties())
        {
            if (property.Value.Type == JTokenType.String)
            {
                changeSign.Add(property.Name, property.Value.Value<string>());
            }
        }
        return changeSign;
    }

    private IDictionary<string, TwinReportedChangeSpec>? GetChangeSpec(JObject jsonObject, JsonSerializer serializer)
    {
        var changeSpec = new Dictionary<string, TwinReportedChangeSpec>();

        foreach (var property in jsonObject.Properties())
        {
            if (property.Value.Type == JTokenType.Object &&
                property.Value["id"] != null && property.Value["patch"] != null)
            {
                var changeSpecKey = property.Name;
                changeSpec.Add(property.Name, CreateTwinChangeSpec(jsonObject, serializer, property.Name));
            }
        }

        return changeSpec;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    private TwinReportedChangeSpec CreateTwinChangeSpec(JObject jsonObject, JsonSerializer serializer, string changeSpecKey)
    {
        var lowerPropName = FirstLetterToLowerCase($"{changeSpecKey}");
        var upperPropName = FirstLetterToUpperCase($"{changeSpecKey}");
        var changeSpec = new TwinReportedChangeSpec()
        {
            Id = (jsonObject.SelectToken($"{lowerPropName}.id") ?? jsonObject.SelectToken($"{upperPropName}.Id"))?.Value<string>(),
            Patch = GetDynamicPatch(jsonObject, lowerPropName, upperPropName, serializer)
        };
        return changeSpec;
    }

    private Dictionary<string, TwinActionReported[]> GetDynamicPatch(JObject jsonObject, string lowerPropName, string upperPropName, JsonSerializer serializer)
    {
        var dynamicPatch = new Dictionary<string, TwinActionReported[]>();

        var patchToken = jsonObject.SelectToken($"{lowerPropName}.patch") ?? jsonObject.SelectToken($"{upperPropName}.Patch");

        if (patchToken is JObject patchObject)
        {
            foreach (var property in patchObject.Properties())
            {
                dynamicPatch.Add(property.Name, property.Value.ToObject<TwinActionReported[]>(serializer));
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