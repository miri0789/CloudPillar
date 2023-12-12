using Microsoft.Azure.Storage.Blob;

namespace Backend.BlobStreamer.Wrappers.Interfaces;

public interface ICloudStorageWrapper
{
    CloudBlobContainer GetBlobContainer(string storageConnectionString, string blobContainerName);
    Task<CloudBlockBlob> GetBlockBlobReference(CloudBlobContainer storageContainer, string fileName);

    long GetBlobLength(CloudBlockBlob cloudBlockBlob);
}