using System.Text.Json.Serialization;

namespace Shared.Entities.Messages;
public class SignEvent : D2CMessage
{    
    public SignEvent()
    {
        this.MessageType = D2CMessageType.SignTwinKey;
    }
}