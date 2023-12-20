using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;

public interface IFileDownloadHandler
{
    Task InitFileDownloadAsync(ActionToReport actionToReport, CancellationToken cancellationToken);
    Task HandleDownloadMessageAsync(DownloadBlobChunkMessage message, CancellationToken cancellationToken);
}