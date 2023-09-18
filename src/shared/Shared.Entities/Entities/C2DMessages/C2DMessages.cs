
namespace Shared.Entities.Messages;

public enum C2DMessageType
{
    DownloadChunk,
    UploadChunk
}

public abstract class C2DMessages
{
    public C2DMessageType MessageType { get; set; }
    public string ActionId { get; set; } //TODO - set
    public byte[] Data { get; set; }
    public abstract string GetMessageId();

}

