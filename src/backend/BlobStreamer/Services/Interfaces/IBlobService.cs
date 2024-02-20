using Microsoft.Azure.Storage.Blob;
using Shared.Entities.Messages;
using Shared.Entities.QueueMessages;

namespace Backend.BlobStreamer.Services.Interfaces;

public interface IBlobService
{
    Task<BlobProperties> GetBlobMetadataAsync(string fileName);
    Task<byte[]> GetFileBytes(string fileName);
    Task SendDownloadErrorAsync(string deviceId, string changeSpecId, string fileName, int actionIndex, string error);
    Task<bool> SendRangeByChunksAsync(SendRangeByChunksQueueMessage queueSendRange);
    Task<byte[]> CalculateHashAsync(string deviceId, SignFileEvent signFileEvent);
    Task SendFileDownloadAsync(FileDownloadQueueMessage data);
}