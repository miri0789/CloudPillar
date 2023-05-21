namespace blobstreamer.Models;

public class EndBlobRangeMessage: BaseMessage
{
    public override MessageType messageType
    {
        get { return messageType; }
        set { messageType = MessageType.End; }
    }
    public int RangeIndex { get; set; }
    public string Filename { get; set; }

    public override string GetMessageId()
    {
        return $"{this.Filename}_{this.RangeIndex}";
    }
}

