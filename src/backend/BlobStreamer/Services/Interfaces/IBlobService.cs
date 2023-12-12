using Microsoft.Azure.Storage.Blob;

namespace Backend.BlobStreamer.Services.Interfaces;

public interface IBlobService
{
    Task<BlobProperties> GetBlobMetadataAsync(string fileName);
    Task<byte[]> GetFileBytes(string fileName);
    Task SendRangeByChunksAsync(string deviceId, string fileName, int chunkSize, int rangeSize, 
    int rangeIndex, long startPosition, string ActionId, int rangesCount);
}