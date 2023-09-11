using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Shared.Entities.Events;

public class StreamingUploadChunkEvent : AgentEvent
{

    public string AbsolutePath { get; set; }
    public int ChunkSize { get; set; }

    [DefaultValue(0)]
    public long StartPosition { get; set; }

    public long? EndPosition { get; set; }
    public byte[] Data { get; set; }

    [JsonConstructor]
    public StreamingUploadChunkEvent()
    {
        this.EventType = EventType.StreamingUploadChunk;
    }
}