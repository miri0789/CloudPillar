using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Newtonsoft.Json;
using Shared.Entities.Events;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class D2CMessengerHandler : ID2CMessengerHandler
{
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly ILoggerHandler _logger;

    public D2CMessengerHandler(IDeviceClientWrapper deviceClientWrapper, ILoggerHandler logger)
    {
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper)); ;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
    }

    public async Task SendFirmwareUpdateEventAsync(string fileName, string actionId, long? startPosition = null, long? endPosition = null)
    {
        // Deduct the chunk size based on the protocol being used
        int chunkSize = _deviceClientWrapper.GetChunkSizeByTransportType();

        var firmwareUpdateEvent = new FirmwareUpdateEvent()
        {
            FileName = fileName,
            ChunkSize = chunkSize,
            StartPosition = startPosition ?? 0,
            EndPosition = endPosition,
            ActionId = actionId
        };

        await SendMessageAsync(firmwareUpdateEvent);
    }

    public async Task SendStreamingUploadChunkEventAsync(Stream readStream, Uri storageUri, string actionId, string correlationId, long startPosition = 0, long? endPosition = null, CancellationToken cancellationToken = default)
    {
        int chunkSize = _deviceClientWrapper.GetChunkSizeByTransportType();
        int totalChunks = CalculateTotalChunks(readStream.Length, chunkSize);

        FileUploadCompletionNotification notification = InitializeNotification(correlationId);

        try
        {
            _logger.Info($"Start send messages with chunks. Total chunks is: {totalChunks}");

            await HandleUploadChunkAsync(readStream, storageUri, actionId, totalChunks, chunkSize);

            await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            notification.IsSuccess = false;
            await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
            _logger.Error($"SendStreamingUploadChunkEventAsync failed: {ex.Message}");
        }
    }

    private FileUploadCompletionNotification InitializeNotification(string correlationId)
    {
        return new FileUploadCompletionNotification()
        {
            IsSuccess = true,
            CorrelationId = correlationId
        };
    }

    private int CalculateTotalChunks(long streamLength, int chunkSize)
    {
        return (int)Math.Ceiling((double)streamLength / chunkSize);
    }

    private async Task HandleUploadChunkAsync(Stream readStream, Uri storageUri, string actionId, int totalChunks, int chunkSize)
    {
        int chunkIndex = 1;
        long currentPosition = 0;
        long streamLength = readStream.Length;

        while (currentPosition < streamLength)
        {
            _logger.Debug($"Agent: Start send Chunk number: {chunkIndex}, with position: {currentPosition}");

            await ProcessChunkAsync(readStream, storageUri, actionId, totalChunks, chunkIndex, chunkSize, currentPosition);

            currentPosition += chunkSize;
            chunkIndex++;
        }
        if (chunkIndex - 1 == totalChunks && currentPosition >= streamLength)
        {
            _logger.Debug($"All bytes sent successfuly");
        }
    }

    private async Task ProcessChunkAsync(Stream readStream, Uri storageUri, string actionId, int totalChunks, int chunkIndex, int chunkSize, long currentPosition)
    {
        long remainingBytes = readStream.Length - currentPosition;
        long bytesToUpload = Math.Min(chunkSize, remainingBytes);

        byte[] buffer = new byte[bytesToUpload];
        await readStream.ReadAsync(buffer, 0, (int)bytesToUpload);

        var streamingUploadChunkEvent = new StreamingUploadChunkEvent()
        {
            StorageUri = storageUri,
            ChunkIndex = chunkIndex,
            StartPosition = currentPosition,
            ActionId = actionId ?? Guid.NewGuid().ToString(),
            Data = buffer
        };

        var properties = new Dictionary<string, string>
        {
            { "chunk_index", chunkIndex.ToString() },
            { "total_chunks", totalChunks.ToString() },
        };

        await SendMessageAsync(streamingUploadChunkEvent, properties);
    }

    private async Task SendMessageAsync(AgentEvent agentEvent, Dictionary<string, string>? properties = null)
    {
        var messageString = JsonConvert.SerializeObject(agentEvent);
        Message message = new Message(Encoding.ASCII.GetBytes(messageString));
        if (properties != null)
        {
            foreach (var prop in properties)
            {
                message.Properties.Add(prop.Key, prop.Value);
            }
        }

        await _deviceClientWrapper.SendEventAsync(message);
    }
}