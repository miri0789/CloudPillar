using Microsoft.Azure.Storage.Blob;
using Shared.Entities.Messages;
using Shared.Entities.Factories;
using Shared.Logger;
using Backend.BlobStreamer.Services.Interfaces;
using Backend.BlobStreamer.Wrappers.Interfaces;
using Shared.Entities.Services;
using Microsoft.Azure.Devices;
using Backend.Infra.Common.Services.Interfaces;
using System.Security.Cryptography;

namespace Backend.BlobStreamer.Services;

public class BlobService : IBlobService
{
    private readonly CloudBlobContainer _container;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ICloudStorageWrapper _cloudStorageWrapper;
    private readonly IDeviceConnectService _deviceConnectService;
    private readonly IMessageFactory _messageFactory;
    private readonly ILoggerHandler _logger;
    private readonly ICheckSumService _checkSumService;

    public BlobService(IEnvironmentsWrapper environmentsWrapper, ICloudStorageWrapper cloudStorageWrapper,
     IDeviceConnectService deviceConnectService, ICheckSumService checkSumService, ILoggerHandler logger, IMessageFactory messageFactory)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _cloudStorageWrapper = cloudStorageWrapper ?? throw new ArgumentNullException(nameof(cloudStorageWrapper));
        _deviceConnectService = deviceConnectService ?? throw new ArgumentNullException(nameof(deviceConnectService));
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));

        _container = cloudStorageWrapper.GetBlobContainer(_environmentsWrapper.storageConnectionString, _environmentsWrapper.blobContainerName);
    }

    public async Task<BlobProperties> GetBlobMetadataAsync(string fileName)
    {
        var blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);
        return blockBlob.Properties;
    }

    public async Task<byte[]> GetBlobContentAsync(string fileName)
    {
        var blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);
        var fileSize = _cloudStorageWrapper.GetBlobLength(blockBlob);
        var data = new byte[fileSize];
        await blockBlob.DownloadRangeToByteArrayAsync(data, 0, 0, fileSize);
        return data;
    }

    public async Task<string> GetFileCheckSum(string fileName)
    {
        var data = await GetBlobContentAsync(fileName);
        return await _checkSumService.CalculateCheckSumAsync(data);
    }

    public async Task<byte[]> CalculateHashAsync(string filePath, int bufferSize)
    {
        var blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, filePath);
        var fileSize = (int)_cloudStorageWrapper.GetBlobLength(blockBlob);
        bufferSize = fileSize < bufferSize ? fileSize : bufferSize;

        using (SHA256 sha256 = SHA256.Create())
        {
            try
            {
                long offset = 0;
                while (offset < fileSize)
                {
                    var length = Math.Min(bufferSize, fileSize - offset);
                    var data = new byte[length];
                    await blockBlob.DownloadRangeToByteArrayAsync(data, 0, offset, length);
                    sha256.TransformBlock(data, 0, (int)length, null, 0);
                    offset += bufferSize;
                }
                sha256.TransformFinalBlock(new byte[0], 0, 0);
                return sha256.Hash;
            }
            catch (Exception ex)
            {
                _logger.Error($"CalculateHashAsync failed.", ex);
                throw;
            }
        }
    }

    public async Task SendRangeByChunksAsync(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition, string ActionId, string fileCheckSum)
    {
        var blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);
        var fileSize = _cloudStorageWrapper.GetBlobLength(blockBlob);
        chunkSize = GetMaxChunkSize(chunkSize);
        var rangeBytes = new List<byte>();
        var rangeEndPosition = Math.Min(rangeSize + startPosition, fileSize);
        List<Message> messages = new List<Message>();
        for (long offset = startPosition, chunkIndex = 0; offset < rangeEndPosition; offset += chunkSize, chunkIndex++)
        {
            var length = Math.Min(chunkSize, rangeEndPosition - offset);
            var data = new byte[length];
            await blockBlob.DownloadRangeToByteArrayAsync(data, 0, offset, length);
            rangeBytes.AddRange(data);
            var blobMessage = new DownloadBlobChunkMessage
            {
                RangeIndex = rangeIndex,
                ChunkIndex = (int)chunkIndex,
                Offset = offset,
                FileName = fileName,
                ActionId = ActionId,
                FileSize = fileSize,
                Data = data
            };
            if (offset + chunkSize >= rangeEndPosition)
            {
                blobMessage.RangeStartPosition = startPosition;
                blobMessage.RangeEndPosition = rangeEndPosition;
                blobMessage.RangesCount = rangesCount;
                blobMessage.RangeCheckSum = await _checkSumService.CalculateCheckSumAsync(rangeBytes.ToArray());
            }

            messages.Add(_messageFactory.PrepareC2DMessage(blobMessage, _environmentsWrapper.messageExpiredMinutes));
        }
        await _deviceConnectService.SendDeviceMessagesAsync(messages.ToArray(), deviceId);
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

}