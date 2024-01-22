namespace Shared.Entities.Messages;

public enum D2CMessageType
{
    FirmwareUpdateReady,
    SignTwinKey,
    SignFileKey,
    StreamingUploadChunk,
    ProvisionDeviceCertificate,
    RemoveDevice,
}

public class D2CMessage
{
    public D2CMessageType MessageType { get; set; }
    public int ActionIndex { get; set; }
}