namespace blobstreamer.Models;

public class StartBlobMessage: BaseMessage
{
    public override MessageType messageType
    {
        get { return MessageType.Start; }
        set { messageType = MessageType.Start; }
    }
    public long BlobLength { get; set; }
    public string Filename { get; set; }

    public override string GetMessageId()
    {
        return this.Filename;
    }
}

