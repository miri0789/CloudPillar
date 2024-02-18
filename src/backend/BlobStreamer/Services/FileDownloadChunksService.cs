using System.Text;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Backend.BlobStreamer.Enums;
using Backend.BlobStreamer.Services.Interfaces;
using Backend.BlobStreamer.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.Entities.Messages;
using Shared.Entities.QueueMessages;
using Shared.Logger;

namespace Backend.BlobStreamer.Services;

public class FileDownloadChunksService : IFileDownloadChunksService
{
    private readonly ILoggerHandler _logger;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly IBlobService _blobService;
    private readonly IQueueMessagesService _queueMessagesService;
    private const string SERVICE_BUS_CONNECTION_STRING = "Endpoint=sb://cpyaelsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=OL0VzYmJ4XAVctHje5snF93wpKdWf1H9w+ASbL2UW6w=";
    private const string QUEUE_NAME = "cp-yael-sb-q";
    private bool isActiveQueue = false;

    public FileDownloadChunksService(ILoggerHandler logger, IEnvironmentsWrapper environmentsWrapper, IBlobService blobService, IQueueMessagesService queueMessagesService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
        _queueMessagesService = queueMessagesService ?? throw new ArgumentNullException(nameof(queueMessagesService));
    }

    public async Task SendFileDownloadAsync(string deviceId, FileDownloadMessage data)
    {
        if (!isActiveQueue)
        {
            GetMessageFromQueue(deviceId);
        }

    }

    private async Task GetMessageFromQueue(string deviceId)
    {
        isActiveQueue = true;
        var client = new ServiceBusClient(SERVICE_BUS_CONNECTION_STRING);

        await using (ServiceBusReceiver receiver = client.CreateReceiver(QUEUE_NAME))
        {
            while (true)
            {

                ServiceBusReceivedMessage message = await receiver.ReceiveMessageAsync();
                var messageString = Encoding.UTF8.GetString(message.Body);
                JObject jsonObject = JObject.Parse(Encoding.UTF8.GetString(message.Body));
                int messageType = (int)jsonObject["MessageType"];
                switch (messageType)
                {
                    case (int)QueueMessageType.FileDownloadReady:
                        var FileMessageBody = JsonConvert.DeserializeObject<FileDownloadMessage>(messageString);
                        await SendFileDownloadAsync2(deviceId, FileMessageBody);
                        break;
                    case (int)QueueMessageType.SendRangeByChunks:
                        var RangeMessageBody = JsonConvert.DeserializeObject<SendRangeByChunksMessage>(messageString);
                        await _blobService.SendRangeByChunksAsync(deviceId, RangeMessageBody);
                        break;
                }
                await receiver.CompleteMessageAsync(message);
            }
        }

        // await client.DisposeAsync();
    }

    private async Task SendFileDownloadAsync2(string deviceId, FileDownloadMessage data)
    {
        ArgumentNullException.ThrowIfNull(data.ChangeSpecId);
        try
        {
            var blobProperties = await _blobService.GetBlobMetadataAsync(data.FileName);
            var blobSize = blobProperties.Length;
            ArgumentNullException.ThrowIfNull(blobSize);

            long rangeSize = GetRangeSize((long)blobSize, data.ChunkSize);
            var rangesCount = Math.Ceiling((decimal)blobSize / rangeSize);
            if (data.EndPosition != null)
            {
                rangeSize = (long)data.EndPosition - data.StartPosition;
                await sendRange(data.ChangeSpecId, data.FileName, data.ChunkSize, (int)rangeSize, int.Parse(data.CompletedRanges), data.StartPosition, data.ActionIndex, (int)rangesCount);
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
                            await sendRange(data.ChangeSpecId, data.FileName, data.ChunkSize, (int)rangeSize, rangeIndex, data.StartPosition, data.ActionIndex, (int)rangesCount);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"FileDownloadService SendFileDownloadAsync failed. Message: {ex.Message}");
            await _blobService.SendDownloadErrorAsync(deviceId, data.ChangeSpecId, data.FileName, data.ActionIndex, ex.Message);
        }
    }

    private async Task sendRange(string changeSpecId, string fileName, int chunkSize, int rangeSize, int rangeIndex, long startPosition, int actionIndex, int rangesCount)
    {
        var queueSendRange = new SendRangeByChunksMessage()
        {
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
}