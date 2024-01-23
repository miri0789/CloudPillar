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

    public BlobService(IEnvironmentsWrapper environmentsWrapper, ICloudStorageWrapper cloudStorageWrapper,
     IDeviceConnectService deviceConnectService, ICheckSumService checkSumService, ILoggerHandler logger, IMessageFactory messageFactory,
     IDeviceClientWrapper deviceClientWrapper)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _cloudStorageWrapper = cloudStorageWrapper ?? throw new ArgumentNullException(nameof(cloudStorageWrapper));
        _deviceConnectService = deviceConnectService ?? throw new ArgumentNullException(nameof(deviceConnectService));
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));

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

    public async Task<bool> SendRangeByChunksAsync(string deviceId, string changeSpecId, string fileName, int chunkSize, int rangeSize,
    int rangeIndex, long startPosition, int actionIndex, int rangesCount)
    {
        var blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);
        var fileSize = _cloudStorageWrapper.GetBlobLength(blockBlob);
        chunkSize = GetMaxChunkSize(chunkSize);
        var rangeBytes = new List<byte>();
        var rangeEndPosition = Math.Min(rangeSize + startPosition, fileSize);
        var data = new byte[chunkSize];
        using (var serviceClient = _deviceClientWrapper.CreateFromConnectionString())
        {
            for (long offset = startPosition, chunkIndex = 0; offset < rangeEndPosition; offset += chunkSize, chunkIndex++)
            {
                var length = Math.Min(chunkSize, rangeEndPosition - offset);
                if (length != chunkSize) { data = new byte[length]; }
                await blockBlob.DownloadRangeToByteArrayAsync(data, 0, offset, length);
                rangeBytes.AddRange(data);
                var blobMessage = new DownloadBlobChunkMessage
                {
                    RangeIndex = rangeIndex,
                    ChunkIndex = (int)chunkIndex,
                    Offset = offset,
                    FileName = fileName,
                    ActionIndex = actionIndex,
                    FileSize = fileSize,
                    ChangeSpecId = changeSpecId,
                    Data = data
                };
                if (offset + chunkSize >= rangeEndPosition)
                {
                    blobMessage.RangeStartPosition = startPosition;
                    blobMessage.RangeEndPosition = rangeEndPosition;
                    blobMessage.RangesCount = rangesCount;
                    blobMessage.RangeCheckSum = await _checkSumService.CalculateCheckSumAsync(rangeBytes.ToArray());
                }
                try
                {
                    var c2dMsg = _messageFactory.PrepareC2DMessage(blobMessage, _environmentsWrapper.messageExpiredMinutes);
                    await _deviceConnectService.SendDeviceMessageAsync(serviceClient, c2dMsg, deviceId);
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