using Shared.Entities.Events;

namespace Shared.Entities.Messages;

public class UploadBlobChunkMessage : BaseMessage
{
    public EventType EventType { get; set; }
    public string AbsolutePath { get; set; }
    public int ChunkIndex { get; set; }
    public int RangeIndex { get; set; }
    public long Offset { get; set; }
    public byte[] Data { get; set; }
    public override string GetMessageId()
    {
        return $"{this.AbsolutePath}_{this.ChunkIndex}";
    }

    public UploadBlobChunkMessage()
    {
        this.MessageType = MessageType.UploadChunk;
    }
}
