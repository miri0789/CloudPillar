
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using Shared.Logger;
using Shared.Entities.Services;
using CloudPillar.Agent.Entities;

namespace CloudPillar.Agent.Handlers;

public class StreamingFileUploaderHandler : IStreamingFileUploaderHandler
{
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly ICheckSumService _checkSumService;
    private readonly ITwinActionsHandler _twinActionsHandler;
    private readonly ILoggerHandler _logger;

    public StreamingFileUploaderHandler(ID2CMessengerHandler d2CMessengerHandler, IDeviceClientWrapper deviceClientWrapper, ICheckSumService checkSumService, ITwinActionsHandler twinActionsHandler, ILoggerHandler logger)
    {
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _twinActionsHandler = twinActionsHandler ?? throw new ArgumentNullException(nameof(twinActionsHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task UploadFromStreamAsync(ActionToReport actionToReport, Stream readStream, Uri storageUri, string actionId, string correlationId, CancellationToken cancellationToken)
    {
        int chunkSize = _deviceClientWrapper.GetChunkSizeByTransportType();
        int totalChunks = CalculateTotalChunks(readStream.Length, chunkSize);

        FileUploadCompletionNotification notification = new FileUploadCompletionNotification()
        {
            IsSuccess = true,
            CorrelationId = correlationId
        };

        try
        {
            _logger.Info($"Start send messages with chunks. Total chunks is: {totalChunks}");

            await HandleUploadChunkAsync(actionToReport, readStream, storageUri, actionId, chunkSize, cancellationToken);

            await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            notification.IsSuccess = false;
            await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
            _logger.Error($"SendStreamingUploadChunkEventAsync failed: {ex.Message}");
        }
    }

    private async Task HandleUploadChunkAsync(ActionToReport actionToReport, Stream readStream, Uri storageUri, string actionId, int chunkSize, CancellationToken cancellationToken)
    {
        long streamLength = readStream.Length;
        string checkSum = await CalcAndUpdateCheckSumAsync(actionToReport, readStream, cancellationToken);

        for (int currentPosition = 0, chunkIndex = 1; currentPosition < streamLength; currentPosition += chunkSize, chunkIndex++)
        {
            _logger.Debug($"Agent: Start send chunk Index: {chunkIndex}, with position: {currentPosition}");

            var isLastMessage = IsLastMessage(currentPosition, chunkSize, streamLength);
            await ProcessChunkAsync(actionToReport, readStream, storageUri, actionId, chunkSize, currentPosition, isLastMessage ? checkSum : string.Empty, cancellationToken);
        }
        _logger.Debug($"All bytes sent successfuly");
    }

    private async Task ProcessChunkAsync(ActionToReport actionToReport, Stream readStream, Uri storageUri, string actionId, int chunkSize, long currentPosition, string checkSum, CancellationToken cancellationToken)
    {
        long remainingBytes = readStream.Length - currentPosition;
        long bytesToUpload = Math.Min(chunkSize, remainingBytes);

        byte[] buffer = new byte[bytesToUpload];
        await readStream.ReadAsync(buffer, 0, (int)bytesToUpload);

        await CalcAndUpdatePercentsAsync(actionToReport, readStream, currentPosition, bytesToUpload, cancellationToken);

        await _d2CMessengerHandler.SendStreamingUploadChunkEventAsync(buffer, storageUri, actionId, currentPosition, checkSum);
    }

    private int CalculateTotalChunks(long streamLength, int chunkSize)
    {
        return (int)Math.Ceiling((double)streamLength / chunkSize);
    }

    private async Task<string> CalcAndUpdateCheckSumAsync(ActionToReport actionToReport, Stream stream, CancellationToken cancellationToken)
    {
        string checkSum = await _checkSumService.CalculateCheckSumAsync(stream);
        _logger.Debug($"checkSum was calculated, The checkSum is: {checkSum}");

        actionToReport.TwinReport.CheckSum = checkSum;
        await _twinActionsHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);
        return checkSum;
    }
    private async Task CalcAndUpdatePercentsAsync(ActionToReport actionToReport, Stream uploadStream, long currentPosition, long bytesToUpload, CancellationToken cancellationToken)
    {
        var percents = CalculateByteUploadedPercent(uploadStream, currentPosition, bytesToUpload);

        actionToReport.TwinReport.Progress = percents;
        await _twinActionsHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);
    }

    private float CalculateByteUploadedPercent(Stream uploadStream, long currentPosition, long bytesToUpload)
    {
        var totalSize = uploadStream.Length;
        bytesToUpload += currentPosition;
        float progressPercent = (float)Math.Round(bytesToUpload / (double)totalSize * 100, 2);

        Console.WriteLine($"Upload Progress: {progressPercent:F2}%");
        return progressPercent;
    }
    private bool IsLastMessage(long currentPosition, int chunkSize, long streamLength)
    {
        return currentPosition + chunkSize >= streamLength;
    }
}
