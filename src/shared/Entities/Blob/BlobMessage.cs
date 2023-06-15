
namespace shared.Entities.Blob;

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
    public string FileName { get; set; }

    public override string GetMessageId()
    {
        return $"{this.FileName}_{this.RangeIndex}_{this.ChunkIndex}";
    }
}

