using System.Threading.Tasks;
using blobstreamer.Contracts;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace blobstreamer.Services
{
    public class BlobService : IBlobService
    {
        private readonly CloudBlobContainer _container;

        public BlobService()
        {
            string StorageConnectionString = Environment.GetEnvironmentVariable(Constants.storageConnectionString)!;
            string blobContainerName = Environment.GetEnvironmentVariable(Constants.blobContainerName);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            _container = blobClient.GetContainerReference(blobContainerName);
        }

        public async Task<BlobProperties> GetBlobMeatadataAsync(string filename)
        {
            CloudBlockBlob blockBlob = _container.GetBlockBlobReference(filename);
            await blockBlob.FetchAttributesAsync();
            return blockBlob.Properties;
        }
    }
}
