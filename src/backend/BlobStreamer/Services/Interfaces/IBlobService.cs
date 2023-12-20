using Microsoft.Azure.Storage.Blob;

namespace Backend.BlobStreamer.Services.Interfaces;

public interface IBlobService
{
    Task<BlobProperties> GetBlobMetadataAsync(string fileName);
    Task<byte[]> GetFileBytes(string fileName);
    Task SendDownloadErrorAsync(string deviceId, string fileName, int actionIndex, string error);
    Task<bool> SendRangeByChunksAsync(string deviceId, string fileName, int chunkSize, int rangeSize,
    int rangeIndex, long startPosition, int actionIndex, int rangesCount);
    Task<byte[]> CalculateHashAsync(string filePath, int bufferSize);
}