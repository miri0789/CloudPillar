
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Shared.Entities.Twin;

public class TwinActionConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(TwinAction[]);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);
        var desiredActions = new List<TwinAction>();

        if (token.Type == JTokenType.Array)
        {
            foreach (var item in token)
            {
                var desiredAction = CreateTwinAction(item);
                if (desiredAction != null)
                {
                    desiredActions.Add(desiredAction);
                }
            }
        }

        return desiredActions.ToArray();
    }

    private TwinAction CreateTwinAction(JToken token)
    {
        string actionTypeString = (token["action"] ?? token["Action"])?.Value<string>();
        if (!Enum.TryParse<TwinActionType>(actionTypeString, out TwinActionType actionType))
        {
            return null;
        }

        switch (actionType)
        {
            case TwinActionType.ExecuteOnce:
                return token.ToObject<ExecuteAction>();
            case TwinActionType.PeriodicUpload:
                return token.ToObject<UploadAction>();
            case TwinActionType.SingularDownload:
                return token.ToObject<DownloadAction>();
            case TwinActionType.SingularUpload:
                return token.ToObject<UploadAction>();
            default:
                return token.ToObject<TwinAction>();

        }
    }
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

}
