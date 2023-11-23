namespace Shared.Entities.Messages;

public class DeleteDiagnosticsBlobEvent : D2CMessage
{

    public Uri StorageUri { get; set; }

    public DeleteDiagnosticsBlobEvent()
    {
        this.MessageType = D2CMessageType.DeleteDiagnosticsBlob;
    }
}