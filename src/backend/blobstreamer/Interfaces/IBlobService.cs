using Microsoft.Azure.Storage.Blob;

namespace blobstreamer.Interfaces;

public interface IBlobService
{
    Task<BlobProperties> GetBlobMetadataAsync(string fileName);

    Task SendRangeByChunksAsync(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition, Guid actionGuid, long fileSize);
}