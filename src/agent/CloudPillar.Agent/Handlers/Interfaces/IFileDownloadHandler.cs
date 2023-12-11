using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IFileDownloadHandler
{
    Task InitFileDownloadAsync(ActionToReport actionToReport, CancellationToken cancellationToken);
    Task HandleDownloadMessageAsync(DownloadBlobChunkMessage message, CancellationToken cancellationToken);
}