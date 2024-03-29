using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Backend.BlobStreamer.Wrappers.Interfaces;

namespace Backend.BlobStreamer.Wrappers;


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
    public long GetBlobLength(CloudBlockBlob cloudBlockBlob)
    {
        return cloudBlockBlob.Properties.Length;
    }

    public async Task DownloadRangeToByteArrayAsync(CloudBlockBlob cloudBlockBlob, byte[] data, int index, long? blobOffset, long? length)
    {
        await cloudBlockBlob.DownloadRangeToByteArrayAsync(data, index, blobOffset, length);
    }
}