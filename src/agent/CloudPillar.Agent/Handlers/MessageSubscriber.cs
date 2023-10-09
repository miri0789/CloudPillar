using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public class MessageSubscriber : IMessageSubscriber
{
    private readonly IFileDownloadHandler _fileDownloadHandler;
    private readonly IReProvisioningHandler _reProvisioningHandler;

    public MessageSubscriber(IFileDownloadHandler fileDownloadHandler, IReProvisioningHandler reProvisioningHandler)
    {
        _fileDownloadHandler = fileDownloadHandler ?? throw new ArgumentNullException(nameof(fileDownloadHandler));
        _reProvisioningHandler = reProvisioningHandler ?? throw new ArgumentNullException(nameof(reProvisioningHandler));
    }

    public async Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message)
    {
        return await _fileDownloadHandler.HandleDownloadMessageAsync(message);
    }

    public async Task HandleReProvisioningMessageAsync(ReProvisioningMessage message)
    {
       await _reProvisioningHandler.HandleReProvisioningMessageAsync(message);
    }
}
