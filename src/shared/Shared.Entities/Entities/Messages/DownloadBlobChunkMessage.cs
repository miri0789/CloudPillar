
using System.Text.Json.Serialization;

namespace shared.Entities.Messages;

public class DownloadBlobChunkMessage : BaseMessage
{
    public int RangeIndex { get; set; }
    public int ChunkIndex { get; set; }
    public long Offset { get; set; }
    public string FileName { get; set; }
    public int? RangeSize { get; set; }
    public long FileSize { get; set; }

    public override string GetMessageId()
    {
        return $"{this.FileName}_{this.RangeIndex}_{this.ChunkIndex}";
    }

    [JsonConstructor]
    public DownloadBlobChunkMessage()
    {
        this.MessageType = MessageType.DownloadChunk;
    }
}

