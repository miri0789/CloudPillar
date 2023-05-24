
namespace blobstreamer.Models;

public class BlobMessage : BaseMessage
{
    public override MessageType messageType
    {
        get { return MessageType.Chunk; }
        set { messageType = MessageType.Chunk; }
    }
    public int RangeIndex { get; set; }
    public int ChunkIndex { get; set; }
    public long Offset { get; set; }
    public string Filename { get; set; }

    public override string GetMessageId()
    {
        return $"{this.Filename}_{this.RangeIndex}_{this.ChunkIndex}";
    }
}

