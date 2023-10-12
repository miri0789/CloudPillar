using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public class MessageSubscriber : IMessageSubscriber
{
    private readonly IFileDownloadHandler _fileDownloadHandler;
    private readonly IReprovisioningHandler _reProvisioningHandler;

    public MessageSubscriber(IFileDownloadHandler fileDownloadHandler, IReprovisioningHandler reProvisioningHandler)
    {
        _fileDownloadHandler = fileDownloadHandler ?? throw new ArgumentNullException(nameof(fileDownloadHandler));
        _reProvisioningHandler = reProvisioningHandler ?? throw new ArgumentNullException(nameof(reProvisioningHandler));
    }

    public async Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message)
    {
        return await _fileDownloadHandler.HandleDownloadMessageAsync(message);
    }

    public async Task HandleReprovisioningMessageAsync(ReprovisioningMessage message, CancellationToken cancellationToken)
    {
       await _reProvisioningHandler.HandleReprovisioningMessageAsync(message, cancellationToken);
    }

    public Task HandleRequestDeviceCertificate(RequestDeviceCertificateMessage message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
