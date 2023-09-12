using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Shared.Entities.Events;

public class StreamingUploadChunkEvent : AgentEvent
{

    public Uri StorageUri { get; set; }
    public int ChunkIndex { get; set; }
    public long StartPosition { get; set; } = 0;

    public byte[] Data { get; set; }

    [JsonConstructor]
    public StreamingUploadChunkEvent()
    {
        this.EventType = EventType.StreamingUploadChunk;
        this.ActionId = Guid.NewGuid().ToString();
    }
}