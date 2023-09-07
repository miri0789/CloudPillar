using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;
using Polly;
using Shared.Entities.Messages;
using Shared.Entities.Factories;
using Backend.BlobStreamer.Interfaces;
using Shared.Logger;

namespace Backend.BlobStreamer.Services;



public class BlobService : IBlobService
{
    private readonly CloudBlobContainer _container;
    private readonly ServiceClient _serviceClient;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ICloudStorageWrapper _cloudStorageWrapper;
    private readonly IDeviceClientWrapper _deviceClient;
    private readonly IMessageFactory _MessageFactory;
    private readonly ILoggerHandler _logger;

    public BlobService(IEnvironmentsWrapper environmentsWrapper, ICloudStorageWrapper cloudStorageWrapper,
     IDeviceClientWrapper deviceClientWrapper, ILoggerHandler logger, IMessageFactory MessageFactory)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));;
        _cloudStorageWrapper = cloudStorageWrapper ?? throw new ArgumentNullException(nameof(cloudStorageWrapper));;
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));;
        _MessageFactory = MessageFactory ?? throw new ArgumentNullException(nameof(MessageFactory));;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));;

        _container = cloudStorageWrapper.GetBlobContainer(_environmentsWrapper.storageConnectionString, _environmentsWrapper.blobContainerName);
        _serviceClient = _deviceClient.CreateFromConnectionString(_environmentsWrapper.iothubConnectionString);
    }


    public async Task<BlobProperties> GetBlobMetadataAsync(string fileName)
    {
        CloudBlockBlob blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);
        return blockBlob.Properties;
    }

    public async Task SendRangeByChunksAsync(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition, string ActionId, long fileSize)
    {
        CloudBlockBlob blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);
        chunkSize = GetMaxChunkSize(chunkSize);
        for (long offset = startPosition, chunkIndex = 0; offset < rangeSize + startPosition && offset < fileSize; offset += chunkSize, chunkIndex++)
        {
            var rangeEndSize = rangeSize + startPosition > fileSize ? fileSize : rangeSize + startPosition;
            var bytesRemaining = rangeEndSize - offset;
            var length = bytesRemaining > chunkSize ? chunkSize : (int)bytesRemaining;
            var data = new byte[length];
            await blockBlob.DownloadRangeToByteArrayAsync(data, 0, offset, length);

            var blobMessage = new DownloadBlobChunkMessage()
            {
                RangeIndex = rangeIndex,
                ChunkIndex = (int)chunkIndex,
                Offset = offset,
                FileName = fileName,
                ActionId = ActionId,
                FileSize = fileSize,
                Data = data
            };

            if (offset + chunkSize >= rangeSize + startPosition)
            {
                blobMessage.RangeSize = rangeEndSize;
            }

            var c2dMessage = _MessageFactory.PrepareBlobMessage(blobMessage, _environmentsWrapper.messageExpiredMinutes);
            await SendMessage(c2dMessage, deviceId);
        }
    }

    private int GetMaxChunkSize(int chunkSize)
    {
        const int maxEncodedChunkSize = 65535;
        const int reservedOverhead = 500;
        const double reservedOverheadPercent = 3.0 / 4.0;
        int maxChunkSizeBeforeEncoding = (int)((maxEncodedChunkSize - reservedOverhead) * reservedOverheadPercent);
        chunkSize = Math.Min(chunkSize, maxChunkSizeBeforeEncoding);
        return chunkSize;
    }

    private async Task SendMessage(Message c2dMessage, string deviceId)
    {
        try
        {
            var retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(_environmentsWrapper.retryPolicyExponent, retryAttempt => TimeSpan.FromSeconds(Math.Pow(_environmentsWrapper.retryPolicyBaseDelay, retryAttempt)),
                (ex, time) => _logger.Warn($"Failed to send message. Retrying in {time.TotalSeconds} seconds... Error details: {ex.Message}"));
            await retryPolicy.ExecuteAsync(async () => await _deviceClient.SendAsync(_serviceClient, deviceId, c2dMessage));
            _logger.Info($"Blobstreamer SendMessage success. message title: {c2dMessage.MessageId}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Blobstreamer SendMessage failed. Message: {ex.Message}");
        }
    }

}