using System.Text.Json.Serialization;

namespace Shared.Entities.Messages;
public class RemoveDeviceEvent : D2CMessage
{
    [JsonConstructor]
    public RemoveDeviceEvent()
    {
        this.MessageType = D2CMessageType.RemoveDevice;
    }
}