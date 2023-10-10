using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public interface IMessageSubscriber
{
    Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message);

    Task HandleReProvisioningMessageAsync(ReProvisioningMessage message, CancellationToken cancellationToken);
}
