
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Services;
using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

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

    public async Task UploadFromStreamAsync(FileUploadCompletionNotification notification, ActionToReport actionToReport, Stream readStream, Uri storageUri, string correlationId, CancellationToken cancellationToken, bool isRunDiagnostics = false)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            int chunkSize = _deviceClientWrapper.GetChunkSizeByTransportType();
            int totalChunks = CalculateTotalChunks(readStream.Length, chunkSize);


            try
            {
                _logger.Info($"Start send messages with chunks. Total chunks is: {totalChunks}");

                await HandleUploadChunkAsync(notification, actionToReport, readStream, storageUri, chunkSize, isRunDiagnostics, cancellationToken);

                await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
            }
            catch (Exception ex)
            {
                notification.IsSuccess = false;
                await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
                _logger.Error($"SendStreamingUploadChunkEventAsync failed: {ex.Message}");
            }
        }
    }

    private async Task HandleUploadChunkAsync(FileUploadCompletionNotification notification, ActionToReport actionToReport, Stream readStream, Uri storageUri, int chunkSize, bool isRunDiagnostics, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            long streamLength = readStream.Length;
            string checkSum = await CalcAndUpdateCheckSumAsync(actionToReport, readStream, cancellationToken);

            int calculatedPosition = CalculateCurrentPosition(readStream.Length, actionToReport.TwinReport.Progress ?? 0);
            var calculatedChunkIndex = (int)Math.Round(calculatedPosition / (double)chunkSize) + 1;
            byte[] buffer = new byte[chunkSize];
            for (int currentPosition = calculatedPosition, chunkIndex = calculatedChunkIndex; currentPosition < streamLength; currentPosition += chunkSize, chunkIndex++)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    long bytesToUpload = Math.Min(chunkSize, readStream.Length - currentPosition);
                    if (bytesToUpload != chunkSize) { buffer = new byte[bytesToUpload]; }
                    _logger.Debug($"Agent: Start send chunk Index: {chunkIndex}, with position: {currentPosition}");

                    var isLastMessage = IsLastMessage(currentPosition, chunkSize, streamLength);
                    await ProcessChunkAsync(notification, actionToReport, readStream, storageUri, buffer, currentPosition, isLastMessage ? checkSum : string.Empty, isRunDiagnostics, cancellationToken);
                }
            }
            _logger.Debug($"All bytes sent successfuly");
        }
    }

    private async Task ProcessChunkAsync(FileUploadCompletionNotification notification, ActionToReport actionToReport, Stream readStream, Uri storageUri, byte[] buffer, long currentPosition, string checkSum, bool isRunDiagnostics, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            readStream.Seek(currentPosition, SeekOrigin.Begin);
            _logger.Info($"Seek readStream to position: {currentPosition}");
            await readStream.ReadAsync(buffer, 0, buffer.Length);

            await _d2CMessengerHandler.SendStreamingUploadChunkEventAsync(buffer, storageUri, currentPosition, checkSum, cancellationToken, isRunDiagnostics);

            var percents = CalculateByteUploadedPercent(readStream.Length, currentPosition, buffer.Length);
            await UpdateReportedDetailsAsync(actionToReport, percents, notification.CorrelationId, cancellationToken);
        }
    }

    private int CalculateTotalChunks(long streamLength, int chunkSize)
    {
        return (int)Math.Ceiling((double)streamLength / chunkSize);
    }

    private async Task<string> CalcAndUpdateCheckSumAsync(ActionToReport actionToReport, Stream stream, CancellationToken cancellationToken)
    {
        string checkSum = string.Empty;
        if (!cancellationToken.IsCancellationRequested)
        {
            checkSum = await _checkSumService.CalculateCheckSumAsync(stream);
            _logger.Debug($"checkSum was calculated, The checkSum is: {checkSum}");

            actionToReport.TwinReport.CheckSum = checkSum;
            await _twinActionsHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);
        }
        return checkSum;

    }

    private async Task UpdateReportedDetailsAsync(ActionToReport actionToReport, float percents, string correlationId, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            actionToReport.TwinReport.Status = StatusType.InProgress;
            actionToReport.TwinReport.Progress = percents;
            actionToReport.TwinReport.CorrelationId = correlationId;
            await _twinActionsHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);
        }
    }

    private bool IsLastMessage(long currentPosition, int chunkSize, long streamLength)
    {
        return currentPosition + chunkSize >= streamLength;
    }

    private float CalculateByteUploadedPercent(long streamLength, long currentPosition, long bytesToUpload)
    {
        bytesToUpload += currentPosition;
        float progressPercent = (float)Math.Floor((bytesToUpload / (double)streamLength) * 100 * 100) / 100;
        _logger.Info($"Upload Progress: {progressPercent:F2}%");
        return progressPercent;
    }

    private int CalculateCurrentPosition(float streamLength, float progressPercent)
    {
        if (progressPercent == 0)
        {
            return 0;
        }
        int currentPosition = (int)Math.Floor(progressPercent * (float)streamLength / 100);

        _logger.Info($"Current Position: {currentPosition} bytes");
        return currentPosition;
    }

}
