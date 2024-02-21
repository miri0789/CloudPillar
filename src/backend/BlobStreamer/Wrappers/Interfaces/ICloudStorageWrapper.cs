using Microsoft.Azure.Storage.Blob;

namespace Backend.BlobStreamer.Wrappers.Interfaces;

public interface ICloudStorageWrapper
{
    CloudBlobContainer GetBlobContainer(string storageConnectionString, string blobContainerName);
    Task<CloudBlockBlob> GetBlockBlobReference(CloudBlobContainer storageContainer, string fileName);
    Task DownloadRangeToByteArrayAsync(CloudBlockBlob cloudBlockBlob, byte[] data, int index, long? blobOffset, long? length);
    long GetBlobLength(CloudBlockBlob cloudBlockBlob);
    long GetBlobLength(BlobProperties cloudBlockBlobProperties);
}