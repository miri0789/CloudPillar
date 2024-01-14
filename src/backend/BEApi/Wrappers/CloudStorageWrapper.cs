using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Backend.BEApi.Wrappers.Interfaces;


public class CloudStorageWrapper : ICloudStorageWrapper
{
    public CloudBlobContainer GetBlobContainer(string storageConnectionString, string blobContainerName)
    {
        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
        CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
        CloudBlobContainer cloudBlobContainer = blobClient.GetContainerReference(blobContainerName);
        return cloudBlobContainer;
    }

}