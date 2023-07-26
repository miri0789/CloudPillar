using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;

public interface IFileDownloadHandler: IMessageSubscriber
{
    Task InitFileDownloadAsync(ActionToReport action);
    Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message);
}