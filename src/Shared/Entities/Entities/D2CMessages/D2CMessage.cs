namespace Shared.Entities.Messages;

public enum D2CMessageType
{
    FileDownloadReady,
    SignTwinKey,
    SignFileKey,
    StreamingUploadChunk,
    ProvisionDeviceCertificate,
    RemoveDevice,
    QueueRange
}

public class D2CMessage
{
    public D2CMessageType MessageType { get; set; }
    public int ActionIndex { get; set; }
}