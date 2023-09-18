using System.Text.Json.Serialization;

namespace Shared.Entities.Messages;
public class SignEvent : D2CMessage
{
    public string KeyPath { get; set; }

    public string SignatureKey { get; set; }
    
    public SignEvent()
    {
        this.MessageType = D2CMessageType.SignTwinKey;
    }
}