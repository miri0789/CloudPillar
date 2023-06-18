namespace shared.Entities.Blob;

public class EndBlobRangeMessage: BaseMessage
{
    public override MessageType messageType
    {
        get { return MessageType.End; }
        set { messageType = MessageType.End; }
    }
    public int RangeIndex { get; set; }
    public string FileName { get; set; }

    public override string GetMessageId()
    {
        return $"{this.FileName}_{this.RangeIndex}";
    }
}

