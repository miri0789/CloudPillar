using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;
using Polly;
using shared.Entities.Blob;
using Shared.Logger;

namespace blobstreamer.Services
{
    public interface IBlobService
    {
        Task<BlobProperties> GetBlobMetadataAsync(string fileName);

        Task SendRangeByChunksAsync(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition);
    }


    public class BlobService : IBlobService
    {
        private readonly CloudBlobContainer _container;
        private readonly ServiceClient _serviceClient;
        private readonly ILoggerHandler _logger;

        public BlobService(ILoggerHandler logger)
        {
            string StorageConnectionString = Environment.GetEnvironmentVariable(Constants.storageConnectionString)!;
            string blobContainerName = Environment.GetEnvironmentVariable(Constants.blobContainerName);
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            _container = blobClient.GetContainerReference(blobContainerName);

            string IotHubConnectionString = Environment.GetEnvironmentVariable(Constants.iothubConnectionString)!;
            _serviceClient = ServiceClient.CreateFromConnectionString(IotHubConnectionString);

            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
        }


        public async Task<BlobProperties> GetBlobMetadataAsync(string fileName)
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

                var blobMessage = new DownloadBlobChunkMessage()
                {
                    RangeIndex = rangeIndex,
                    ChunkIndex = (int)chunkIndex,
                    Offset = offset,
                    FileName = fileName
                };

                if (offset + chunkSize >= rangeSize + startPosition)
                {
                    blobMessage.RangeSize = rangeSize;
                }

                var c2dMessage = blobMessage.PrepareBlobMessage(data);
                await SendMessage(c2dMessage, deviceId);
            }
        }

        private async Task SendMessage(Message c2dMessage, string deviceId)
        {
            try
            {
                int.TryParse(Environment.GetEnvironmentVariable(Constants.retryPolicyBaseDelay), out int retryPolicyBaseDelay);
                int.TryParse(Environment.GetEnvironmentVariable(Constants.retryPolicyExponent), out int retryPolicyExponent);

                var retryPolicy = Policy.Handle<Exception>()
                    .WaitAndRetryAsync(retryPolicyExponent, retryAttempt => TimeSpan.FromSeconds(Math.Pow(retryPolicyBaseDelay, retryAttempt)),
                        (ex, time) => _logger.Warn($"Failed to send message. Retrying in {time.TotalSeconds} seconds... ", ex));
                await retryPolicy.ExecuteAsync(async () => await _serviceClient.SendAsync(deviceId, c2dMessage));
                _logger.Info($"Blobstreamer SendMessage success. message title: {c2dMessage.MessageId}");
            }
            catch (Exception ex)
            {
                _logger.Warn($"Blobstreamer SendMessage failed.", ex);
            }
        }

    }
}