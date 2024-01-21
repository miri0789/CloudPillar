using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shared.Entities.Twin;

public class TwinReportedConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TwinReported);
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        var changeSpec = new TwinReported()
        {
            ChangeSign = GetChangeSign(jsonObject),
            ChangeSpec = GetChangeSpec(jsonObject, serializer),
            DeviceState = GetDeviceState(jsonObject),
            AgentPlatform = (jsonObject["agentPlatform"] ?? jsonObject["AgentPlatform"])?.Value<string>(),
            SupportedShells = GetSupportShell(jsonObject),
            SecretKey = (jsonObject["secretKey"] ?? jsonObject["SecretKey"])?.Value<string>(),
            Custom = (jsonObject["custom"] ?? jsonObject["Custom"])?.Value<List<TwinReportedCustomProp>>(),
            ChangeSpecId = (jsonObject["changeSpecId"] ?? jsonObject["ChangeSpecId"])?.Value<string>(),
            CertificateValidity = jsonObject["certificateValidity"]?.ToObject<CertificateValidity>(),
            DeviceStateAfterServiceRestart = (jsonObject["deviceStateAfterServiceRestart"] ?? jsonObject["DeviceStateAfterServiceRestart"])?.Value<DeviceStateType>(),
            KnownIdentities = GetKnownIdentities(jsonObject)
        };
        return changeSpec;
    }
    private IDictionary<string, TwinReportedChangeSpec>? GetChangeSpec(JObject jsonObject, JsonSerializer serializer)
    {
        var changeSpec = new Dictionary<string, TwinReportedChangeSpec>();

        foreach (var property in jsonObject.Properties())
        {
            if (property.Value.Type == JTokenType.Object &&
                property.Value["id"] != null && property.Value["patch"] != null)
            {
                changeSpec.Add(property.Name, CreateTwinChangeSpec(jsonObject, serializer, property.Name));
            }
        }

        return changeSpec;
    }

    private IDictionary<string, string>? GetChangeSign(JObject jsonObject)
    {
        var changeSign = new Dictionary<string, string>();
        var changeSpecKeys = getChangeSpecKeys(jsonObject);
        foreach (var property in jsonObject.Properties())
        {
            if (property.Value.Type == JTokenType.String && changeSpecKeys.Contains(property.Name))
            {
                changeSign.Add(property.Name, property.Value.Value<string>());
            }
        }
        return changeSign;
    }

    private DeviceStateType GetDeviceState(JObject jsonObject)
    {
        if (jsonObject["deviceState"] is JValue deviceStateValue)
        {
            if (Enum.TryParse<DeviceStateType>(deviceStateValue.Value?.ToString(), true, out var deviceState))
            {
                return deviceState;
            }
        }
        return default;
    }

    private ShellType[] GetSupportShell(JObject jsonObject)
    {
        var supportShell = new List<ShellType>();
        var supportShellToken = jsonObject["supportedShells"] ?? jsonObject["SupportedShells"];
        if (supportShellToken is JArray supportShellArray)
        {
            foreach (var item in supportShellArray)
            {
                supportShell.Add(Enum.Parse<ShellType>(item.Value<string>(), true));
            }
        }
        return supportShell.ToArray();
    }

    private List<KnownIdentities> GetKnownIdentities(JObject jsonObject)
    {
        var knownIdentities = new List<KnownIdentities>();
        var knownIdentitiesToken = jsonObject["knownIdentities"] ?? jsonObject["KnownIdentities"];
        if (knownIdentitiesToken is JArray supportShellArray)
        {
            foreach (var item in supportShellArray)
            {
                knownIdentities.Add(item.Value<KnownIdentities>());
            }
        }
        return knownIdentities;
    }

    private List<string> getChangeSpecKeys(JObject jsonObject)
    {

        var changeSpecKeys = jsonObject.Properties().Where(property => property.Value.Type == JTokenType.Object &&
                         property.Value["id"] != null && property.Value["patch"] != null)
                         .Select(property => property.Name).ToList();

        return changeSpecKeys;
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