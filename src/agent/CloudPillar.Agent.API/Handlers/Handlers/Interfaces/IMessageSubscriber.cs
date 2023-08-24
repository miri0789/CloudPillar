using CloudPillar.Agent.API.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.API.Handlers;
public interface IMessageSubscriber
{
    Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message);
}
