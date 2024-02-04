namespace Shared.Entities.Messages;
public class SignEvent : D2CMessage
{
    public string ChangeSignKey { get; set; }

    public SignEvent(string changeSignKey)
    {
        this.MessageType = D2CMessageType.SignTwinKey;
        this.ChangeSignKey = changeSignKey;
    }
}