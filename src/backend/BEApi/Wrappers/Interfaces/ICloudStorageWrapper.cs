

using Microsoft.WindowsAzure.Storage.Blob;

namespace Backend.BEApi.Wrappers.Interfaces;

public interface ICloudStorageWrapper
{
    CloudBlobContainer GetBlobContainer(string storageConnectionString, string blobContainerName);
}