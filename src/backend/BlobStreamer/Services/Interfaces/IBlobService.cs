using Microsoft.Azure.Storage.Blob;

namespace Backend.BlobStreamer.Interfaces;

public interface IBlobService
{
    Task<BlobProperties> GetBlobMetadataAsync(string fileName);
    Task<string> GetFileCheckSum(string fileName);
    Task SendRangeByChunksAsync(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition, string ActionId, string fileCheckSum);
}