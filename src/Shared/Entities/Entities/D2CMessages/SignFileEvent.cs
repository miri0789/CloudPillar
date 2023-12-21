namespace Shared.Entities.Messages;
public class SignFileEvent : D2CMessage
{
    public string FileName { get; set; }
    public int BufferSize { get; set; }
    public string PropName { get; set; }
    public TwinPatchChangeSpec ChangeSpec { get; set; }


    public SignFileEvent()
    {
        this.MessageType = D2CMessageType.SignFileKey;
    }
}