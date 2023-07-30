using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public class MessageSubscriber : IMessageSubscriber
{
    private readonly FileDownloadHandler _fileDownloadHandler;
    public MessageSubscriber(FileDownloadHandler fileDownloadHandler)
    {
        ArgumentNullException.ThrowIfNull(fileDownloadHandler);
        _fileDownloadHandler = fileDownloadHandler;
    }

    public async Task HandleDownloadMessageAsync(DownloadBlobChunkMessage message)
    {
        await _fileDownloadHandler.HandleDownloadMessageAsync(message);
    }
}
