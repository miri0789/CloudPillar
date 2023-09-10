using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public class MessageSubscriber : IMessageSubscriber
{
    private readonly IFileDownloadHandler _fileDownloadHandler;
    public MessageSubscriber(IFileDownloadHandler fileDownloadHandler)
    {
        _fileDownloadHandler = fileDownloadHandler ?? throw new ArgumentNullException(nameof(fileDownloadHandler));;
    }

    public async Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message)
    {
        return await _fileDownloadHandler.HandleDownloadMessageAsync(message);
    }
}
