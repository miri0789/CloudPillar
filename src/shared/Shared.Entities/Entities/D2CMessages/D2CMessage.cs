using System.Text.Json.Serialization;

namespace Shared.Entities.Messages;

public enum D2CMessageType
{
    FirmwareUpdateReady,
    SignTwinKey,
    StreamingUploadChunk
}

public abstract class D2CMessage
{
    public D2CMessageType MessageType { get; set; }
    public string ActionId { get; set; } //TODO - set
    public abstract string GetMessageId();

}