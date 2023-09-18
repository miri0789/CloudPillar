
using System.Text.Json.Serialization;

namespace Shared.Entities.Messages;

public class streamingUploadChunkEvent : D2CMessage
{

    public Uri StorageUri { get; set; }
    public int ChunkIndex { get; set; }
    public int TotalChunk { get; set; }
    public long StartPosition { get; set; } = 0;

    public byte[] Data { get; set; }

    public streamingUploadChunkEvent()
    {
        this.MessageType = D2CMessageType.StreamingUploadChunk;
    }   
}