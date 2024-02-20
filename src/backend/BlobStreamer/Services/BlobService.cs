using Microsoft.Azure.Storage.Blob;
using Shared.Entities.Messages;
using Shared.Entities.Factories;
using Shared.Logger;
using Backend.BlobStreamer.Services.Interfaces;
using Backend.BlobStreamer.Wrappers.Interfaces;
using Shared.Entities.Services;
using Backend.Infra.Common.Services.Interfaces;
using System.Security.Cryptography;
using Backend.Infra.Common.Wrappers.Interfaces;
using Shared.Entities.QueueMessages;
using Backend.BlobStreamer.Enums;
using Newtonsoft.Json;

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
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly ISendQueueMessagesService _queueMessagesService;

    public BlobService(IEnvironmentsWrapper environmentsWrapper, ICloudStorageWrapper cloudStorageWrapper,
     IDeviceConnectService deviceConnectService, ICheckSumService checkSumService, ILoggerHandler logger, IMessageFactory messageFactory,
     IDeviceClientWrapper deviceClientWrapper, ISendQueueMessagesService queueMessagesService)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _cloudStorageWrapper = cloudStorageWrapper ?? throw new ArgumentNullException(nameof(cloudStorageWrapper));
        _deviceConnectService = deviceConnectService ?? throw new ArgumentNullException(nameof(deviceConnectService));
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _queueMessagesService = queueMessagesService ?? throw new ArgumentNullException(nameof(queueMessagesService));

        _container = cloudStorageWrapper.GetBlobContainer(_environmentsWrapper.storageConnectionString, _environmentsWrapper.blobContainerName);
    }

    public async Task<BlobProperties> GetBlobMetadataAsync(string fileName)
    {
        var blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);
        return blockBlob.Properties;
    }

    public async Task<byte[]> GetFileBytes(string fileName)
    {
        var blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);
        var fileSize = _cloudStorageWrapper.GetBlobLength(blockBlob);
        var data = new byte[fileSize];
        await _cloudStorageWrapper.DownloadRangeToByteArrayAsync(blockBlob, data, 0, 0, fileSize);
        return data;
    }

    public async Task SendFileDownloadAsync(FileDownloadQueueMessage data)
    {
        ArgumentNullException.ThrowIfNull(data.ChangeSpecId);
        try
        {
            var blobProperties = await GetBlobMetadataAsync(data.FileName);
            var blobSize = blobProperties.Length;
            ArgumentNullException.ThrowIfNull(blobSize);

            long rangeSize = GetRangeSize((long)blobSize, data.ChunkSize);
            var rangesCount = Math.Ceiling((decimal)blobSize / rangeSize);
            if (data.EndPosition != null)
            {
                rangeSize = (long)data.EndPosition - data.StartPosition;
                await sendRange(data.DeviceId, data.ChangeSpecId, data.FileName, data.ChunkSize, (int)rangeSize, int.Parse(data.CompletedRanges), data.StartPosition, data.ActionIndex, (int)rangesCount);
            }
            else
            {
                long offset = data.StartPosition;
                var existRanges = (data.CompletedRanges ?? "").Split(',').ToList();
                var rangeIndex = 0;
                while (offset < blobSize)
                {
                    _logger.Info($"FileDownloadService Send ranges to blob streamer, range index: {rangeIndex}");
                    var requests = new List<Task<bool>>();
                    for (var i = 0; requests.Count < 4 && offset < blobSize; i++, offset += rangeSize, rangeIndex++)
                    {
                        if (existRanges.IndexOf(rangeIndex.ToString()) == -1)
                        {
                            await sendRange(data.DeviceId, data.ChangeSpecId, data.FileName, data.ChunkSize, (int)rangeSize, rangeIndex, data.StartPosition, data.ActionIndex, (int)rangesCount);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"FileDownloadService SendFileDownloadAsync failed. Message: {ex.Message}");
            await SendDownloadErrorAsync(data.DeviceId, data.ChangeSpecId, data.FileName, data.ActionIndex, ex.Message);
        }
    }

    public async Task<bool> SendRangeByChunksAsync(SendRangeByChunksQueueMessage queueSendRange)
    {
        var blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, queueSendRange.FileName);
        var fileSize = _cloudStorageWrapper.GetBlobLength(blockBlob);
        queueSendRange.ChunkSize = GetMaxChunkSize(queueSendRange.ChunkSize);
        var rangeBytes = new List<byte>();
        var rangeEndPosition = Math.Min(queueSendRange.RangeSize + queueSendRange.RangeStartPosition, fileSize);
        var data = new byte[queueSendRange.ChunkSize];
        using (var serviceClient = _deviceClientWrapper.CreateFromConnectionString())
        {
            for (long offset = queueSendRange.RangeStartPosition, chunkIndex = 0; offset < rangeEndPosition; offset += queueSendRange.ChunkSize, chunkIndex++)
            {
                var length = Math.Min(queueSendRange.ChunkSize, rangeEndPosition - offset);
                if (length != queueSendRange.ChunkSize) { data = new byte[length]; }
                await blockBlob.DownloadRangeToByteArrayAsync(data, 0, offset, length);
                rangeBytes.AddRange(data);
                var blobMessage = new DownloadBlobChunkMessage
                {
                    RangeIndex = queueSendRange.RangeIndex,
                    ChunkIndex = (int)chunkIndex,
                    Offset = offset,
                    FileName = queueSendRange.FileName,
                    ActionIndex = queueSendRange.ActionIndex,
                    FileSize = fileSize,
                    ChangeSpecId = queueSendRange.ChangeSpecId,
                    Data = data
                };
                if (offset + queueSendRange.ChunkSize >= rangeEndPosition)
                {
                    blobMessage.RangeStartPosition = queueSendRange.RangeStartPosition;
                    blobMessage.RangeEndPosition = rangeEndPosition;
                    blobMessage.RangesCount = queueSendRange.RangesCount;
                    blobMessage.RangeCheckSum = await _checkSumService.CalculateCheckSumAsync(rangeBytes.ToArray());
                }
                try
                {
                    var c2dMsg = _messageFactory.PrepareC2DMessage(blobMessage, _environmentsWrapper.messageExpiredMinutes);
                    await _deviceConnectService.SendDeviceMessageAsync(serviceClient, c2dMsg, queueSendRange.DeviceId);
                }
                catch (Exception ex)
                {
                    _logger.Error($"BlobService SendRangeByChunksAsync failed. Message: {ex.Message}");
                    return false;
                }
            }
        }
        return true;
    }

    public async Task<byte[]> CalculateHashAsync(string deviceId, SignFileEvent signFileEvent)
    {
        try
        {
            var blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, signFileEvent.FileName);
            var fileSize = _cloudStorageWrapper.GetBlobLength(blockBlob);

            using (SHA256 sha256 = SHA256.Create())
            {
                try
                {
                    long offset = 0;
                    var data = new byte[signFileEvent.BufferSize];
                    while (offset < fileSize)
                    {
                        var length = Math.Min(signFileEvent.BufferSize, fileSize - offset);
                        await blockBlob.DownloadRangeToByteArrayAsync(data, 0, offset, length);
                        sha256.TransformBlock(data, 0, (int)length, null, 0);
                        offset += signFileEvent.BufferSize;
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
        catch (Exception ex)
        {
            await SendDownloadErrorAsync(deviceId, signFileEvent.ChangeSpecId, signFileEvent.FileName, signFileEvent.ActionIndex, ex.Message);
            throw;
        }
    }

    public async Task SendDownloadErrorAsync(string deviceId, string changeSpecId, string fileName, int actionIndex, string error)
    {
        using (var serviceClient = _deviceClientWrapper.CreateFromConnectionString())
        {
            var blobMessage = new DownloadBlobChunkMessage
            {
                FileName = fileName,
                ActionIndex = actionIndex,
                Error = error,
                ChangeSpecId = changeSpecId,
                Data = new byte[0]
            };
            try
            {
                var c2dMsg = _messageFactory.PrepareC2DMessage(blobMessage, _environmentsWrapper.messageExpiredMinutes);
                await _deviceConnectService.SendDeviceMessageAsync(serviceClient, c2dMsg, deviceId);
            }
            catch (Exception ex)
            {
                _logger.Error($"BlobService SendErrorAsync failed. Message: {ex.Message}");
            }
        }
    }

    private async Task sendRange(string deviceId, string changeSpecId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition, int actionIndex, int rangesCount)
    {
        var queueSendRange = new SendRangeByChunksQueueMessage()
        {
            DeviceId = deviceId,
            ActionIndex = actionIndex,
            ChangeSpecId = changeSpecId,
            ChunkSize = chunkSize,
            FileName = fileName,
            MessageType = QueueMessageType.SendRangeByChunks,
            RangeIndex = rangeIndex,
            RangesCount = rangesCount,
            RangeSize = rangeSize,
            RangeStartPosition = startPosition
        };
        await _queueMessagesService.SendMessageToQueue(JsonConvert.SerializeObject(queueSendRange));
    }

    private long GetRangeSize(long blobSize, int chunkSize)
    {

        if (_environmentsWrapper.rangeCalculateType == RangeCalculateType.Bytes && _environmentsWrapper.rangeBytes != 0)
        {
            return _environmentsWrapper.rangeBytes > chunkSize ? _environmentsWrapper.rangeBytes : chunkSize;
        }
        else if (_environmentsWrapper.rangeCalculateType == RangeCalculateType.Percent && _environmentsWrapper.rangePercent != 0)
        {
            var rangeSize = blobSize / 100 * _environmentsWrapper.rangePercent;
            return rangeSize > chunkSize ? rangeSize : chunkSize;
        }

        return blobSize;
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