using Microsoft.Azure.Storage.Blob;
using Shared.Entities.Messages;

namespace Backend.BlobStreamer.Services.Interfaces;

public interface IBlobService
{
    Task<BlobProperties> GetBlobMetadataAsync(string fileName);
    Task<byte[]> GetFileBytes(string fileName);
    Task SendDownloadErrorAsync(string deviceId, string changeSpecId, string fileName, int actionIndex, string error);
    Task<bool> SendRangeByChunksAsync(string deviceId, string changeSpecId, string fileName, int chunkSize, int rangeSize,
    int rangeIndex, long startPosition, int actionIndex, int rangesCount);
    Task<byte[]> CalculateHashAsync(string deviceId, SignFileEvent signFileEvent, CloudBlockBlob blob = null);
}