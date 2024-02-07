namespace Shared.Entities.Messages;
public class SignFileEvent : D2CMessage
{
    public string FileName { get; set; }
    public int BufferSize { get; set; }
    public string PropName { get; set; }
    public string ChangeSpecKey { get; set; }
    public string ChangeSpecId { get; set; }

    public SignFileEvent()
    {
        this.MessageType = D2CMessageType.SignFileKey;
    }
}