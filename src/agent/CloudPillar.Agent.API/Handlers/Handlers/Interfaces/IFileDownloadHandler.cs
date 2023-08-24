using CloudPillar.Agent.API.Entities;
using Shared.Entities.Messages;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.API.Handlers;

public interface IFileDownloadHandler: IMessageSubscriber
{
    Task InitFileDownloadAsync(DownloadAction downloadAction, ActionToReport actionToReport);
    Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message);
}