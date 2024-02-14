using Shared.Entities.Messages;

namespace Backend.BlobStreamer.Services;

public interface IFileDownloadChunksService
{
    Task SendFileDownloadAsync(string deviceId, FileDownloadEvent data);
}