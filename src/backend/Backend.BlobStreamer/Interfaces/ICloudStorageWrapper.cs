using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;


namespace Backend.BlobStreamer.Interfaces;


public interface ICloudStorageWrapper
{
    CloudBlobContainer GetBlobContainer(string storageConnectionString, string blobContainerName);
    Task<CloudBlockBlob> GetBlockBlobReference(CloudBlobContainer storageContainer, string fileName);

}