using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;
using Polly;
using Shared.Entities.Messages;
using Backend.BlobStreamer.Interfaces;

namespace Backend.BlobStreamer.Services;


public class CloudStorageWrapper : ICloudStorageWrapper
{
    public CloudBlobContainer GetBlobContainer(string storageConnectionString, string blobContainerName)
    {
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
        CloudBlobContainer cloudBlobContainer = blobClient.GetContainerReference(blobContainerName);
        return cloudBlobContainer;
    }

    public async Task<CloudBlockBlob> GetBlockBlobReference(CloudBlobContainer storageContainer, string fileName)
    {
        CloudBlockBlob blockBlob = storageContainer.GetBlockBlobReference(fileName);
        await blockBlob.FetchAttributesAsync();
        return blockBlob;
    }

}