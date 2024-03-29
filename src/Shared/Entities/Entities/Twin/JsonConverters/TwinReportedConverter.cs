﻿using Newtonsoft.Json;
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
            ChangeSign = GetChangeSign(jsonObject),
            ChangeSpec = GetChangeSpec(jsonObject, serializer),
            DeviceState = GetDeviceState(jsonObject, "deviceState"),
            AgentPlatform = (jsonObject["agentPlatform"] ?? jsonObject["AgentPlatform"])?.Value<string>() ?? string.Empty,
            SupportedShells = GetSupportShell(jsonObject),
            SecretKey = (jsonObject["secretKey"] ?? jsonObject["SecretKey"])?.Value<string>() ?? string.Empty,
            Custom = (jsonObject["custom"] ?? jsonObject["Custom"])?.ToObject<Dictionary<string, object>>(),
            ChangeSpecId = (jsonObject["changeSpecId"] ?? jsonObject["ChangeSpecId"])?.Value<string>() ?? string.Empty,
            CertificateValidity = (jsonObject["certificateValidity"] ?? jsonObject["CertificateValidity"])?.ToObject<CertificateValidity>(),
            DeviceStateAfterServiceRestart = GetDeviceState(jsonObject, "deviceStateAfterServiceRestart"),
            KnownIdentities = GetKnownIdentities(jsonObject)
        };
        return changeSpec;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var changeSpec = (TwinReported)value;

        writer.WriteStartObject();
        foreach (var changeSpecItem in changeSpec.GetType().GetProperties())
        {
            switch (changeSpecItem.Name)
            {
                case "ChangeSign" when changeSpec?.ChangeSign is not null:
                    foreach (var changeSpecOption in changeSpec.ChangeSign)
                    {
                        writer.WritePropertyName(changeSpecOption.Key);
                        serializer.Serialize(writer, changeSpecOption.Value);
                    }
                    break;
                case "ChangeSpec" when changeSpec?.ChangeSpec is not null:
                    foreach (var changeSpecOption in changeSpec.ChangeSpec)
                    {
                        writer.WritePropertyName(changeSpecOption.Key);
                        serializer.Serialize(writer, changeSpecOption.Value);
                    }
                    break;
                default:
                    if (changeSpecItem.GetValue(changeSpec) is not null)
                    {
                        writer.WritePropertyName(changeSpecItem.Name);
                        serializer.Serialize(writer, changeSpecItem.GetValue(changeSpec));
                        return;
                    }

                    break;
            }
        }
        writer.WriteEndObject();
    }
    private Dictionary<string, TwinReportedChangeSpec>? GetChangeSpec(JObject jsonObject, JsonSerializer serializer)
    {
        var changeSpec = new Dictionary<string, TwinReportedChangeSpec>();

        foreach (var property in jsonObject.Properties())
        {
            if (property.Value.Type == JTokenType.Object &&
                (property.Value["patch"] ?? property.Value["Patch"]) != null)
            {
                changeSpec.Add(property.Name, CreateTwinChangeSpec(jsonObject, serializer, property.Name));
            }
        }

        return changeSpec;
    }

    private Dictionary<string, string>? GetChangeSign(JObject jsonObject)
    {
        var changeSign = new Dictionary<string, string>();
        foreach (var property in jsonObject.Properties())
        {
            if (property.Value.Type == JTokenType.String && property.Name.EndsWith("Sign"))
            {
                var value = property.Value.Value<string>();
                if (value is not null)
                {
                    changeSign.Add(property.Name, value);
                }
            }
        }
        return changeSign;
    }

    private DeviceStateType? GetDeviceState(JObject jsonObject, string propertyName)
    {
        var lowerPropName = FirstLetterToLowerCase(propertyName);
        var upperPropName = FirstLetterToUpperCase(propertyName);

        if ((jsonObject[lowerPropName] ?? jsonObject[upperPropName]) is JValue deviceStateValue)
        {
            if (Enum.TryParse<DeviceStateType>(deviceStateValue.Value?.ToString(), true, out var deviceState))
            {
                return deviceState;
            }
        }
        return null;
    }


    private ShellType[] GetSupportShell(JObject jsonObject)
    {
        var supportShell = new List<ShellType>();
        var supportShellToken = jsonObject["supportedShells"] ?? jsonObject["SupportedShells"];
        if (supportShellToken is JArray supportShellArray)
        {
            foreach (var item in supportShellArray)
            {
                var value = item.Value<string>();
                if (value is not null)
                {
                    supportShell.Add(Enum.Parse<ShellType>(value, true));
                }
            }
        }
        return supportShell.ToArray();
    }

    private List<KnownIdentities> GetKnownIdentities(JObject jsonObject)
    {
        var knownIdentities = new List<KnownIdentities>();
        var knownIdentitiesToken = jsonObject["knownIdentities"] ?? jsonObject["KnownIdentities"];
        if (knownIdentitiesToken is JArray knownIdentitiesArray)
        {
            foreach (var item in knownIdentitiesArray)
            {
                knownIdentities.Add(
                    new KnownIdentities((item["subject"] ?? item["Subject"])?.Value<string>() ?? string.Empty,
                    (item["thumbprint"] ?? item["Thumbprint"])?.Value<string>() ?? string.Empty,
                   ((item["validThru"] ?? item["ValidThru"])?.Value<string>()) ?? string.Empty)
                   );
            }
        }
        return knownIdentities;
    }

    private TwinReportedChangeSpec CreateTwinChangeSpec(JObject jsonObject, JsonSerializer serializer, string changeSpecKey)
    {
        var lowerPropName = FirstLetterToLowerCase($"{changeSpecKey}");
        var upperPropName = FirstLetterToUpperCase($"{changeSpecKey}");
        var changeSpec = new TwinReportedChangeSpec()
        {
            Id = (jsonObject.SelectToken($"{lowerPropName}.id") ?? jsonObject.SelectToken($"{upperPropName}.Id"))?.Value<string>() ?? string.Empty,
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
                var actions = property.Value.ToObject<TwinActionReported[]>(serializer);
                if (actions is not null)
                {
                    dynamicPatch.Add(property.Name, actions);
                }
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