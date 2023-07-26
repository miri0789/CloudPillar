using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public interface IMessageSubscriber
{
    Task HandleDownloadMessageAsync(DownloadBlobChunkMessage message);
}
