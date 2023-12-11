using System.Collections.Concurrent;
using System.Diagnostics;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Services;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace CloudPillar.Agent.Handlers;

public class FileDownloadHandler : IFileDownloadHandler
{
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly IStrictModeHandler _strictModeHandler;
    private readonly ITwinActionsHandler _twinActionsHandler;
    private readonly ISignatureHandler _signatureHandler;
    private static readonly ConcurrentBag<FileDownload> _filesDownloads = new ConcurrentBag<FileDownload>();


    private readonly ILoggerHandler _logger;
    private readonly ICheckSumService _checkSumService;
    private readonly SignFileSettings _signFileSettings;

    public FileDownloadHandler(IFileStreamerWrapper fileStreamerWrapper,
                               ID2CMessengerHandler d2CMessengerHandler,
                               IStrictModeHandler strictModeHandler,
                               ITwinActionsHandler twinActionsHandler,
                               ILoggerHandler loggerHandler,
                               ICheckSumService checkSumService,
                               ISignatureHandler signatureHandler,
                                IOptions<SignFileSettings> options)
    {
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _strictModeHandler = strictModeHandler ?? throw new ArgumentNullException(nameof(strictModeHandler));
        _twinActionsHandler = twinActionsHandler ?? throw new ArgumentNullException(nameof(twinActionsHandler));
        _signatureHandler = signatureHandler ?? throw new ArgumentNullException(nameof(signatureHandler));
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _signFileSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task InitFileDownloadAsync(ActionToReport actionToReport, CancellationToken cancellationToken)
    {
        if (downloadAction.Sign is null)
        {
            actionToReport.TwinReport.Status = StatusType.SentForSignature;
            await _twinActionsHandler.UpdateReportActionAsync(new List<ActionToReport> { actionToReport }, cancellationToken);

            SignFileEvent signFileEvent = new SignFileEvent()
            {
                MessageType = D2CMessageType.SignFileKey,
                ActionId = downloadAction.ActionId,
                FileName = downloadAction.Source,
                BufferSize = _signFileSettings.BufferSize
            };
            await _d2CMessengerHandler.SendSignFileEventAsync(signFileEvent, cancellationToken);
        }
        else
        {
            actionToReport.TwinReport.Status = StatusType.Pending;
            await _twinActionsHandler.UpdateReportActionAsync(new List<ActionToReport> { actionToReport }, cancellationToken);
            _filesDownloads.Add(new FileDownload
            {
                DownloadAction = downloadAction,
                Report = actionToReport,
                Stopwatch = new Stopwatch()
            });
            await _d2CMessengerHandler.SendFirmwareUpdateEventAsync(cancellationToken, downloadAction.Source, downloadAction.ActionId);
        }

    }

    public async Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message, CancellationToken cancellationToken)
    {
        var file = _filesDownloads.FirstOrDefault(item => item.DownloadAction.ActionId == message.ActionId &&
                                    item.DownloadAction.Source == message.FileName);
        if (file == null)
        {
            throw new ArgumentException($"There is no active download for message {message.GetMessageId()}");
        }
        try
        {
            ArgumentNullException.ThrowIfNullOrEmpty(fileDownload.Action.DestinationPath);
            var isFileExist = _fileStreamerWrapper.FileExists(fileDownload.TempPath ?? fileDownload.Action.DestinationPath);
            if (!isFileExist && fileDownload.Action.Unzip) // create unzip temp file
            {
                InitUnzipPath(fileDownload);
            }
            if (actionToReport.TwinReport.Status == StatusType.InProgress && !isFileExist) // init inprogress file if it not exist
            {
                fileDownload.TotalBytesDownloaded = 0;
                fileDownload.Report.Progress = 0;
                fileDownload.Report.Status = StatusType.Pending;
            }
            if (actionToReport.TwinReport.Status == StatusType.Unzip) // file download complete , only need to unzio it
            {
                await HandleCompletedDownloadAsync(fileDownload, cancellationToken);
            }
            else
            {
                var existRanges = GetExistRangesList(fileDownload.Report.CompletedRanges); // get next range for downloading
                var currentRangeIndex = !existRanges.Contains(0) ? 0 : existRanges.FirstOrDefault(n => !existRanges.Contains(n + 1) && n != 0) + 1;
                if (currentRangeIndex > 0 && isFileExist && fileDownload.TotalBytesDownloaded == 0)
                {
                    fileDownload.TotalBytesDownloaded = _fileStreamerWrapper.GetFileLength(fileDownload.TempPath ?? fileDownload.Action.DestinationPath);
                }
                await _d2CMessengerHandler.SendFirmwareUpdateEventAsync(cancellationToken, fileDownload.Action.Source, fileDownload.Action.ActionId, currentRangeIndex);
            }
        }
        catch (Exception ex)
        {
            HandleDownloadException(ex, fileDownload);
        }

        await _fileStreamerWrapper.WriteChunkToFileAsync(filePath, message.Offset, message.Data);
        file.Report.TwinReport.Progress = CalculateBytesDownloadedPercent(file, message.Data.Length, message.Offset);
        if (message?.RangeCheckSum != null)
        {
            // TODO find true way to calculate it
            //  await CheckFullRangeBytesAsync(message, filePath);
        }
        if (file.TotalBytesDownloaded == file.TotalBytes)
        {
            file.Stopwatch.Stop();

            var path = file.TempPath is not null ? file.TempPath : file.DownloadAction.DestinationPath;
            var isVerify = await _signatureHandler.VerifyFileSignatureAsync(path, file.DownloadAction.Sign);
            if (isVerify)
            {
                file.Report.TwinReport.Status = StatusType.Success;
            }
            else
            {
                _fileStreamerWrapper.DeleteFile(file.DownloadAction.DestinationPath);
                file.Report.TwinReport.Status = StatusType.Failed;
            }

            if (!string.IsNullOrEmpty(file.TempPath))
            {
                try
                {
                    file.Report.TwinReport.Status = StatusType.Unzip;
                    await _twinActionsHandler.UpdateReportActionAsync(Enumerable.Repeat(file.Report, 1), cancellationToken);
                    await _fileStreamerWrapper.UnzipFileAsync(filePath, file.DownloadAction.DestinationPath);
                    _fileStreamerWrapper.DeleteFile(file.TempPath);
                }
                catch (Exception ex)
                {
                    file.Report.TwinReport.Status = StatusType.Failed;
                    file.Report.TwinReport.ResultCode = ex.Message;
                    return file.Report;
                }
            }
        }
        else
        {
            file.Report.TwinReport.Status = StatusType.InProgress;
        }
        return file.Report;
    }

    private void HandleFirstMessageAsync(FileDownload file, DownloadBlobChunkMessage message)
    {
        file.TotalBytes = message.FileSize;
        _strictModeHandler.CheckSizeStrictMode(TwinActionType.SingularDownload, file.TotalBytes, file.Action.DestinationPath);
        file.Stopwatch.Start();
    }

    private async Task HandleCompletedDownloadAsync(FileDownload file, CancellationToken cancellationToken)
    {
        file.Stopwatch?.Stop();
        if (!string.IsNullOrWhiteSpace(file.TempPath))
        {
            file.Report.Status = StatusType.Unzip;
            await _twinActionsHandler.UpdateReportActionAsync(Enumerable.Repeat(file.ActionReported, 1), cancellationToken);
            await _fileStreamerWrapper.UnzipFileAsync(file.TempPath, file.Action.DestinationPath);
            _fileStreamerWrapper.DeleteFile(file.TempPath);
        }
        file.Report.Status = StatusType.Success;
        file.Report.Progress = 100;
    }

    private async Task HandleEndRangeDownloadAsync(string filePath, DownloadBlobChunkMessage message, FileDownload file, CancellationToken cancellationToken)
    {
        var isRangeValid = await VerifyRangeCheckSumAsync(filePath, (long)message.RangeStartPosition, (long)message.RangeEndPosition, message.RangeCheckSum);
        if (!isRangeValid)
        {
            await _d2CMessengerHandler.SendFirmwareUpdateEventAsync(cancellationToken, message.FileName, message.ActionId, 0, message.RangeStartPosition, message.RangeEndPosition);
            file.TotalBytesDownloaded -= (long)(message.RangeEndPosition - message.RangeStartPosition);
            if (file.TotalBytesDownloaded < 0) { file.TotalBytesDownloaded = 0; }
        }
        else
        {
            file.Report.CompletedRanges = AddRange(file.Report.CompletedRanges, message.RangeIndex);
        }
    }
    private FileDownload GetDownloadFile(string actionId, string fileName)
    {
        var file = _filesDownloads.FirstOrDefault(item => item.Action.ActionId == actionId &&
                                    item.Action.Source == fileName);
        return file;
    }

    public async Task HandleDownloadMessageAsync(DownloadBlobChunkMessage message, CancellationToken cancellationToken)
    {
        var file = GetDownloadFile(message.ActionId, message.FileName);
        if (file == null)
        {
            _logger.Error($"There is no active download for message {message.GetMessageId()}");
            return;
        }
        var filePath = file.TempPath ?? file.Action.DestinationPath;
        try
        {
            if (!file.Stopwatch.IsRunning)
            {
                HandleFirstMessageAsync(file, message);
            }

            await _fileStreamerWrapper.WriteChunkToFileAsync(filePath, message.Offset, message.Data);

            if (message.RangeCheckSum != null)
            {
                await HandleEndRangeDownloadAsync(filePath, message, file, cancellationToken);
            }
            if (file.Report.CompletedRanges == (message.RangesCount == 1 ? "0" : $"0-{message.RangesCount - 1}"))
            {
                await HandleCompletedDownloadAsync(file, cancellationToken);
            }
            else
            {
                file.Report.Progress = CalculateBytesDownloadedPercent(file, message.Data.Length, message.Offset);
                file.Report.Status = StatusType.InProgress;
            }
        }
        catch (Exception ex)
        {
            HandleDownloadException(ex, file);
        }
        finally
        {
            await SaveReportAsync(file, cancellationToken);
        }
    }

    private async Task SaveReportAsync(FileDownload file, CancellationToken cancellationToken)
    {
        await _twinActionsHandler.UpdateReportActionAsync(Enumerable.Repeat(file.ActionReported, 1), cancellationToken);
        if (file.Report.Status == StatusType.Failed || file.Report.Status == StatusType.Success)
        {
            RemoveFileFromList(file.Action.ActionId, file.Action.Source);
        }
    }

    private float CalculateBytesDownloadedPercent(FileDownload file, long bytesLength, long offset)
    {
        const double KB = 1024.0;
        file.TotalBytesDownloaded += bytesLength;
        double progressPercent = Math.Round(file.TotalBytesDownloaded / (double)file.TotalBytes * 100, 2);
        double throughput = file.TotalBytesDownloaded / file.Stopwatch.Elapsed.TotalSeconds / KB;
        _logger.Info($"%{progressPercent:00} @pos: {offset:00000000000} Throughput: {throughput:0.00} KiB/s");
        return Math.Min((float)progressPercent, 100);
    }

    private async Task VerifyRangeCheckSum()
    {
        var streamCheckSum = await _checkSumService.CalculateCheckSumAsync(data);
        return checkSum == streamCheckSum;

    }

    /// <summary>
    /// Modifies a string of numerical ranges by adding a specified integer to the existing ranges.
    /// </summary>
    /// <param name="rangesString">String of comma-separated numerical ranges.</param>
    /// <param name="rangeIndex">Integer to be added to the ranges.</param>
    /// <returns>Modified and sorted string of numerical ranges.</returns>
    /// 
    /// <example>
    /// Example usage:
    /// <code>
    /// string modifiedRanges = AddRange("1-5,7,10-12", 9);//"1-5,7,9-12";
    /// </code>
    /// </example>

    private string AddRange(string rangesString, int rangeIndex)
    {
        var ranges = GetExistRangesList(rangesString);

        ranges.Add(rangeIndex);
        ranges.Sort();

        var newRangeString = string.Join(",",
            ranges.Distinct()
                  .Select((value, index) => (value, index))
                  .GroupBy(pair => pair.value - pair.index)
                  .Select(group => group.Select((pair, i) => pair.value))
                  .Select(range => range.Count() == 1 ? $"{range.First()}" :
                                    range.Count() == 2 ? $"{range.First()},{range.Last()}" :
                                    $"{range.First()}-{range.Last()}")
                  .ToList());
        return newRangeString;
    }

    private List<int> GetExistRangesList(string rangesString)
    {
        var ranges = new List<int>();
        if (!string.IsNullOrWhiteSpace(rangesString))
        {
            ranges = rangesString.Split(',')
                .SelectMany(part => part.Contains('-')
                    ? Enumerable.Range(
                        int.Parse(part.Split('-')[0]),
                        int.Parse(part.Split('-')[1]) - int.Parse(part.Split('-')[0]) + 1)
                    : new[] { int.Parse(part) })
                .ToList();
        }
        return ranges;
    }

    private void RemoveFileFromList(string actionId, string fileName)
    {
        var itemsToRemove = _filesDownloads
        .Where(item => item.Action.Source == fileName || item.Action.ActionId == actionId)
        .ToList();

        foreach (var removedItem in itemsToRemove)
        {
            _filesDownloads.TryTake(out _);
        }
    }
}
