namespace Shared.Entities.Messages;

public enum D2CMessageType
{
    FirmwareUpdateReady,
    SignTwinKey
}

public class D2CMessage
{
    public D2CMessageType MessageType { get; set; }
    public string ActionId { get; set; }
}