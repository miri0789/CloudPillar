namespace Shared.Entities.Messages;

public class StreamingUploadChunkEvent : D2CMessage
{

    public Uri StorageUri { get; set; }
    public string DeviceId { get; set; }
    public string CheckSum { get; set; }
    public long StartPosition { get; set; } = 0;
    public byte[] Data { get; set; }

    public StreamingUploadChunkEvent()
    {
        this.MessageType = D2CMessageType.StreamingUploadChunk;
    }   
}