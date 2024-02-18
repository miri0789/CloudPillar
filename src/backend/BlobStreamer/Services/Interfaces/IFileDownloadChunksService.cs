using Shared.Entities.Messages;
using Shared.Entities.QueueMessages;

namespace Backend.BlobStreamer.Services;

public interface IFileDownloadChunksService
{
    Task SendFileDownloadAsync(string deviceId, FileDownloadMessage data);
}