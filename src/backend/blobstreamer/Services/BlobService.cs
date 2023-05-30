using blobstreamer.Contracts;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;
using Polly;
using blobstreamer.Models;
using System.Reflection;

namespace blobstreamer.Services
{
    public class BlobService : IBlobService
    {
        private readonly CloudBlobContainer _container;
        private readonly ServiceClient _serviceClient;

        public BlobService()
        {
            string StorageConnectionString = Environment.GetEnvironmentVariable(Constants.storageConnectionString)!;
            string blobContainerName = Environment.GetEnvironmentVariable(Constants.blobContainerName);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            _container = blobClient.GetContainerReference(blobContainerName);

            string IotHubConnectionString = Environment.GetEnvironmentVariable(Constants.iothubConnectionString)!;
            _serviceClient = ServiceClient.CreateFromConnectionString(IotHubConnectionString);
        }


        public async Task<BlobProperties> GetBlobMeatadataAsync(string fileName)
        {
            CloudBlockBlob blockBlob = _container.GetBlockBlobReference(fileName);
            await blockBlob.FetchAttributesAsync();
            return blockBlob.Properties;
        }

        public async Task SendRangeByChunksAsync(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition)
        {
            CloudBlockBlob blockBlob = _container.GetBlockBlobReference(fileName);

            for (long offset = startPosition, chunkIndex = 0; offset < rangeSize + startPosition; offset += chunkSize, chunkIndex++)
            {
                long bytesRemaining = rangeSize + startPosition - offset;
                int length = bytesRemaining > chunkSize ? chunkSize : (int)bytesRemaining;
                byte[] data = new byte[length];
                await blockBlob.DownloadRangeToByteArrayAsync(data, 0, offset, length);

                var blobMessage = new BlobMessage()
                {
                    RangeIndex = rangeIndex,
                    ChunkIndex = (int)chunkIndex,
                    Offset = offset,
                    Filename = fileName
                };

                var c2dMessage = blobMessage.PrepareBlobMessage(data);
                await SendMessage(c2dMessage, deviceId);
            }

            var endBlobRangeMessage = new EndBlobRangeMessage()
            {
                RangeIndex = rangeIndex,
                Filename = fileName
            };

            var c2dEndRangeMessage = endBlobRangeMessage.PrepareBlobMessage(new byte[0]);
            await SendMessage(c2dEndRangeMessage, deviceId);
        }

        public async Task SendStartBlobMessage(string deviceId, string fileName, long blobLength)
        {
            var startBlobRangeMessage = new StartBlobMessage()
            {
                BlobLength = blobLength,
                Filename = fileName
            };

            var c2dStartBlobMessage = startBlobRangeMessage.PrepareBlobMessage(new byte[0]);
            await SendMessage(c2dStartBlobMessage, deviceId);
        }

        private async Task SendMessage(Message c2dMessage, string deviceId)
        {
            try
            {
                int.TryParse(Environment.GetEnvironmentVariable(Constants.retryPolicyBaseDelay), out int retryPolicyBaseDelay);
                int.TryParse(Environment.GetEnvironmentVariable(Constants.retryPolicyExponent), out int retryPolicyExponent);

                var retryPolicy = Policy.Handle<Exception>()
                    .WaitAndRetryAsync(retryPolicyExponent, retryAttempt => TimeSpan.FromSeconds(Math.Pow(retryPolicyBaseDelay, retryAttempt)),
                        (ex, time) => Console.WriteLine($"Failed to send message. Retrying in {time.TotalSeconds} seconds... Error details: {ex.Message}"));
                await retryPolicy.ExecuteAsync(async () => await _serviceClient.SendAsync(deviceId, c2dMessage));
                Console.WriteLine($"Blobstreamer SendMessage success. message title: {c2dMessage.MessageId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Blobstreamer SendMessage failed. Message: {ex.Message}");
            }
        }

    }
}