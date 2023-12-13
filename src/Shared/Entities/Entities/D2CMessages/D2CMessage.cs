namespace Shared.Entities.Messages;

public enum D2CMessageType
{
    FirmwareUpdateReady,
    SignTwinKey,
    SignFileKey,
    StreamingUploadChunk,
    ProvisionDeviceCertificate
}

public class D2CMessage
{
    public D2CMessageType MessageType { get; set; }
    public int ActionIndex { get; set; }
}