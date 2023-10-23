using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public class MessageSubscriber : IMessageSubscriber
{
    private readonly IFileDownloadHandler _fileDownloadHandler;
    private readonly IReprovisioningHandler _reprovisioningHandler;

    public MessageSubscriber(IFileDownloadHandler fileDownloadHandler, IReprovisioningHandler reprovisioningHandler)
    {
        _fileDownloadHandler = fileDownloadHandler ?? throw new ArgumentNullException(nameof(fileDownloadHandler));
        _reprovisioningHandler = reprovisioningHandler ?? throw new ArgumentNullException(nameof(reprovisioningHandler));
    }

    public async Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message)
    {
        return await _fileDownloadHandler.HandleDownloadMessageAsync(message);
    }

    public async Task<bool> HandleReprovisioningMessageAsync(ReprovisioningMessage message, CancellationToken cancellationToken)
    {
       return await _reprovisioningHandler.HandleReprovisioningMessageAsync(message, cancellationToken);
    }

    public async Task HandleRequestDeviceCertificateAsync(RequestDeviceCertificateMessage message, CancellationToken cancellationToken)
    {
       await _reprovisioningHandler.HandleRequestDeviceCertificateAsync(message, cancellationToken);
    }
}
