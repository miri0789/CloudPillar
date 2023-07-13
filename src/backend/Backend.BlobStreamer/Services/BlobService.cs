using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;
using Polly;
using shared.Entities.Messages;
using Backend.BlobStreamer.Interfaces;

namespace Backend.BlobStreamer.Services;



public class BlobService : IBlobService
{
    private readonly CloudBlobContainer _container;
    private readonly ServiceClient _serviceClient;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ICloudStorageWrapper _cloudStorageWrapper;
    private readonly IDeviceClientWrapper _deviceClientWrapper;

    public BlobService(IEnvironmentsWrapper environmentsWrapper, ICloudStorageWrapper cloudStorageWrapper,
     IDeviceClientWrapper deviceClientWrapper)
    {
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        ArgumentNullException.ThrowIfNull(cloudStorageWrapper);
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);

        _environmentsWrapper = environmentsWrapper;
        _cloudStorageWrapper = cloudStorageWrapper;
        _deviceClientWrapper = deviceClientWrapper;

        _container = cloudStorageWrapper.GetBlobContainer(_environmentsWrapper.storageConnectionString, _environmentsWrapper.blobContainerName);
        _serviceClient = _deviceClientWrapper.CreateFromConnectionString(_environmentsWrapper.iothubConnectionString);
    }


    public async Task<BlobProperties> GetBlobMetadataAsync(string fileName)
    {
        CloudBlockBlob blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);
        return blockBlob.Properties;
    }

    public async Task SendRangeByChunksAsync(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition, Guid actionGuid, long fileSize)
    {
        CloudBlockBlob blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);

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
                FileName = fileName,
                ActionGuid = actionGuid,
                FileSize = fileSize,
                Data = data
            };

            if (offset + chunkSize >= rangeSize + startPosition)
            {
                blobMessage.RangeSize = rangeSize;
            }

            var c2dMessage = blobMessage.PrepareBlobMessage(_environmentsWrapper.messageExpiredMinutes);
            await SendMessage(c2dMessage, deviceId);
        }
    }

    private async Task SendMessage(Message c2dMessage, string deviceId)
    {
        try
        {
            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(_environmentsWrapper.retryPolicyExponent, retryAttempt => TimeSpan.FromSeconds(Math.Pow(_environmentsWrapper.retryPolicyBaseDelay, retryAttempt)),
                (ex, time) => Console.WriteLine($"Failed to send message. Retrying in {time.TotalSeconds} seconds... Error details: {ex.Message}"));
            await retryPolicy.ExecuteAsync(async () => await _deviceClientWrapper.SendAsync(_serviceClient, deviceId, c2dMessage));
            Console.WriteLine($"Blobstreamer SendMessage success. message title: {c2dMessage.MessageId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Blobstreamer SendMessage failed. Message: {ex.Message}");
        }
    }

}