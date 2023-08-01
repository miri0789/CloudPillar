using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IFileDownloadHandler: IMessageSubscriber
{
    Task InitFileDownloadAsync(DownloadAction downloadAction, ActionToReport actionToReport);
    Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message);
}