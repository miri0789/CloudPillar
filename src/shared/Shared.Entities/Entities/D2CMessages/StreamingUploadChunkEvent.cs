using System.Text.Json.Serialization;

namespace Shared.Entities.Messages;

public class StreamingUploadChunkEvent : D2CMessage
{

    public Uri StorageUri { get; set; }
    public int ChunkIndex { get; set; }
    public int ChunkSum { get; set; }
    public long StartPosition { get; set; } = 0;

    public byte[] Data { get; set; }

    [JsonConstructor]
    public StreamingUploadChunkEvent()
    {
        this.MessageType = D2CMessageType.StreamingUploadChunk;
        this.ActionId = Guid.NewGuid().ToString();
    }
}