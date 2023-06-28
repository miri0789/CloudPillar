
namespace shared.Entities.Messages;

public class DownloadBlobChunkMessage : BaseMessage
{
    public override MessageType MessageType
    {
        get { return MessageType.DownloadChunk; }
        set { MessageType = MessageType.DownloadChunk; }
    }
    public int RangeIndex { get; set; }
    public int ChunkIndex { get; set; }
    public long Offset { get; set; }
    public string FileName { get; set; }
    public int RangeSize { get; set; }

    public override string GetMessageId()
    {
        return $"{this.FileName}_{this.RangeIndex}_{this.ChunkIndex}";
    }
}

