
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class StreamingFileUploaderHandler : IStreamingFileUploaderHandler
{
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly ILoggerHandler _logger;

    public StreamingFileUploaderHandler(ID2CMessengerHandler d2CMessengerHandler, IDeviceClientWrapper deviceClientWrapper, ILoggerHandler logger)
    {
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
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

        for (int currentPosition = 0, chunkIndex = 1; currentPosition < streamLength; currentPosition += chunkSize, chunkIndex++)
        {
            _logger.Debug($"Agent: Start send chunk Index: {chunkIndex}, with position: {currentPosition}");

            await ProcessChunkAsync(readStream, storageUri, actionId, chunkSize, currentPosition);
        }
        _logger.Debug($"All bytes sent successfuly");
    }

    private async Task ProcessChunkAsync(Stream readStream, Uri storageUri, string actionId, int chunkSize, long currentPosition)
    {
        long remainingBytes = readStream.Length - currentPosition;
        long bytesToUpload = Math.Min(chunkSize, remainingBytes);

        byte[] buffer = new byte[bytesToUpload];
        await readStream.ReadAsync(buffer, 0, (int)bytesToUpload);

        await _d2CMessengerHandler.SendStreamingUploadChunkEventAsync(buffer, storageUri, actionId, currentPosition);
        
    }

    private int CalculateTotalChunks(long streamLength, int chunkSize)
    {
        return (int)Math.Ceiling((double)streamLength / chunkSize);
    }
}
