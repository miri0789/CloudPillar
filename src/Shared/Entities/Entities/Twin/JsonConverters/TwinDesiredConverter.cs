﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shared.Entities.Twin;

public class TwinDesiredConverter : JsonConverter
{
    private const string SIGN_KEY = "Sign";

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TwinDesired);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        var changeSpec = new TwinDesired()
        {
            ChangeSign = GetChangeSign(jsonObject, serializer),
            ChangeSpec = GetChangeSpec(jsonObject, serializer)
        };
        return changeSpec;
    }
    private Dictionary<string, string>? GetChangeSign(JObject jsonObject, JsonSerializer serializer)
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

    private Dictionary<string, TwinChangeSpec>? GetChangeSpec(JObject jsonObject, JsonSerializer serializer)
    {
        var changeSpec = new Dictionary<string, TwinChangeSpec>();

        foreach (var property in jsonObject.Properties())
        {
            if (property.Value.Type == JTokenType.Object &&
                (property.Value["id"] ?? property.Value["Id"]) != null &&
                (property.Value["patch"] ?? property.Value["Patch"]) != null)
            {
                changeSpec.Add(property.Name, CreateTwinChangeSpec(jsonObject, serializer, property.Name));
            }
        }

        return changeSpec;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        var changeSpec = (TwinDesired)value;

        writer.WriteStartObject();

        if (changeSpec?.ChangeSign is not null)
        {
            foreach (var changeSpecOption in changeSpec?.ChangeSign)
            {
                writer.WritePropertyName($"{changeSpecOption.Key}{SIGN_KEY}");
                serializer.Serialize(writer, changeSpecOption.Value);
            }
        }
        if (changeSpec?.ChangeSpec is not null)
        {
            foreach (var changeSpecOption in changeSpec.ChangeSpec)
            {
                writer.WritePropertyName(changeSpecOption.Key);
                serializer.Serialize(writer, changeSpecOption.Value);
            }
        }

        writer.WriteEndObject();
    }

    private TwinChangeSpec CreateTwinChangeSpec(JObject jsonObject, JsonSerializer serializer, string changeSpecKey)
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