namespace Shared.Entities.Messages;

public class DeleteBlobEvent : D2CMessage
{
    public Uri StorageUri { get; set; }
    public DeleteBlobEvent()
    {
        this.MessageType = D2CMessageType.DeleteBlob;
    }
}