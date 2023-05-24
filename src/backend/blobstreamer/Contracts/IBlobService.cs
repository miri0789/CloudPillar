using Microsoft.Azure.Storage.Blob;

namespace blobstreamer.Contracts
{
    public interface IBlobService
    {
        Task<BlobProperties> GetBlobMeatadataAsync(string fileName);

        Task SendRangeByChunksAsync(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition);

        Task SendStartBlobMessage(string deviceId, string fileName, long blobLength);
    }
}