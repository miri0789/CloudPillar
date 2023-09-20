
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using Shared.Logger;
using Shared.Entities.Services;
using Shared.Enums;

namespace CloudPillar.Agent.Handlers;

public class StreamingFileUploaderHandler : IStreamingFileUploaderHandler
{
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly ICheckSumService _checkSumService;
    private readonly ILoggerHandler _logger;

    public StreamingFileUploaderHandler(ID2CMessengerHandler d2CMessengerHandler, IDeviceClientWrapper deviceClientWrapper, ICheckSumService checkSumService, ILoggerHandler logger)
    {
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task UploadFromStreamAsync(Stream readStream, Uri storageUri, string actionId, string correlationId, CancellationToken cancellationToken)
    {
        int chunkSize = _deviceClientWrapper.GetChunkSizeByTransportType();
        int totalChunks = CalculateTotalChunks(readStream.Length, chunkSize);

        FileUploadCompletionNotification notification = InitializeNotification(correlationId);

        try
        {
            _logger.Info($"Start send messages with chunks. Total chunks is: {totalChunks}");

            await HandleUploadChunkAsync(readStream, storageUri, actionId, chunkSize);

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

    private async Task HandleUploadChunkAsync(Stream readStream, Uri storageUri, string actionId, int chunkSize)
    {
        long streamLength = readStream.Length;
        string checkSum = await CalcCheckSumAsync(readStream);

        for (int currentPosition = 0, chunkIndex = 1; currentPosition < streamLength; currentPosition += chunkSize, chunkIndex++)
        {
            _logger.Debug($"Agent: Start send chunk Index: {chunkIndex}, with position: {currentPosition}");

            var isLastMessage = IsLastMessage(currentPosition, chunkSize, streamLength);
            await ProcessChunkAsync(readStream, storageUri, actionId, chunkSize, currentPosition, isLastMessage ? checkSum : string.Empty);
        }
        _logger.Debug($"All bytes sent successfuly");
    }

    private async Task ProcessChunkAsync(Stream readStream, Uri storageUri, string actionId, int chunkSize, long currentPosition, string checkSum)
    {
        long remainingBytes = readStream.Length - currentPosition;
        long bytesToUpload = Math.Min(chunkSize, remainingBytes);

        byte[] buffer = new byte[bytesToUpload];
        await readStream.ReadAsync(buffer, 0, (int)bytesToUpload);

        await _d2CMessengerHandler.SendStreamingUploadChunkEventAsync(buffer, storageUri, actionId, currentPosition, checkSum);
    }

    private int CalculateTotalChunks(long streamLength, int chunkSize)
    {
        return (int)Math.Ceiling((double)streamLength / chunkSize);
    }

    private async Task<string> CalcCheckSumAsync(Stream stream)
    {
        string checkSum = await _checkSumService.CalculateCheckSumAsync(stream, CheckSumType.MD5);
        _logger.Debug($"checkSum was calculated, The checkSum is: {checkSum}");
        return checkSum;
    }

    private bool IsLastMessage(long currentPosition, int chunkSize, long streamLength)
    {
        return currentPosition + chunkSize >= streamLength;
    }
}
