using Microsoft.Azure.Storage.Blob;
using Shared.Entities.Messages;
using Shared.Entities.Factories;
using Backend.BlobStreamer.Interfaces;
using Shared.Logger;
using Shared.Entities.Services;
using Backend.Infra.Common;

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

    public BlobService(IEnvironmentsWrapper environmentsWrapper,
                       ICloudStorageWrapper cloudStorageWrapper,
                       IDeviceConnectService deviceConnectService,
                       ILoggerHandler logger,
                       IMessageFactory messageFactory,
                       ICheckSumService checkSumService)
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
        CloudBlockBlob blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);
        return blockBlob.Properties;
    }

    public async Task SendRangeByChunksAsync(string deviceId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition, string ActionId, long fileSize)
    {
        var blockBlob = await _cloudStorageWrapper.GetBlockBlobReference(_container, fileName);
        chunkSize = GetMaxChunkSize(chunkSize);
        var rangeBytes = new List<byte>();
        var rangeEndPosition = Math.Min(rangeSize + startPosition, fileSize);
        using (var serviceClient = _deviceConnectService.CreateFromConnectionString(_environmentsWrapper.iothubConnectionString))
        {
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
                    Data = data,

                };
                if (offset + chunkSize >= rangeEndPosition)
                {
                    blobMessage.RangeStartPosition = startPosition;
                    blobMessage.RangeEndPosition = rangeEndPosition;
                    blobMessage.RangeCheckSum = await _checkSumService.CalculateCheckSumAsync(rangeBytes.ToArray());
                }

                var c2dMessage = _messageFactory.PrepareC2DMessage(blobMessage, _environmentsWrapper.messageExpiredMinutes);
                await _deviceConnectService.SendMessage(serviceClient, c2dMessage, deviceId);
            }
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


}