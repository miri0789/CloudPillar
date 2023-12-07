using System.Collections.Concurrent;
using System.Diagnostics;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Services;

namespace CloudPillar.Agent.Handlers;

public class FileDownloadHandler : IFileDownloadHandler
{
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly IStrictModeHandler _strictModeHandler;
    private readonly ITwinActionsHandler _twinActionsHandler;
    private static readonly ConcurrentBag<FileDownload> _filesDownloads = new ConcurrentBag<FileDownload>();
    private readonly ILoggerHandler _logger;
    private readonly ICheckSumService _checkSumService;

    public FileDownloadHandler(IFileStreamerWrapper fileStreamerWrapper,
                               ID2CMessengerHandler d2CMessengerHandler,
                               IStrictModeHandler strictModeHandler,
                               ITwinActionsHandler twinActionsHandler,
                               ILoggerHandler loggerHandler,
                               ICheckSumService checkSumService)
    {
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _strictModeHandler = strictModeHandler ?? throw new ArgumentNullException(nameof(strictModeHandler));
        _twinActionsHandler = twinActionsHandler ?? throw new ArgumentNullException(nameof(twinActionsHandler));
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
    }

    public async Task InitFileDownloadAsync(ActionToReport actionToReport, CancellationToken cancellationToken)
    {
        var fileDownload = GetDownloadFile(actionToReport.TwinAction.ActionId, ((DownloadAction)actionToReport.TwinAction).Source) ??
         new FileDownload { ActionReported = actionToReport, Stopwatch = new Stopwatch() };
        _filesDownloads.Add(fileDownload);
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
                await HandleCompletedDownload(fileDownload, cancellationToken);
            }
            else
            {
                var existRanges = GetExistRangesList(fileDownload.Report.CompleteRanges); // get next range for downloading
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
        finally
        {
            await SaveReportAsync(fileDownload, cancellationToken);
        }
    }

    private void HandleDownloadException(Exception ex, FileDownload file)
    {
        var filePath = file.TempPath ?? file.Action.DestinationPath;
        file.Report.Status = StatusType.Failed;
        file.Report.ResultText = ex.Message;
        file.Report.ResultCode = ex.GetType().Name;
        _fileStreamerWrapper.DeleteFile(filePath);
    }

    private void InitUnzipPath(FileDownload file)
    {
        if (_fileStreamerWrapper.GetExtension(file.Action.Source).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            var extention = _fileStreamerWrapper.GetExtension(file.Action.DestinationPath);
            if (!string.IsNullOrEmpty(extention))
            {
                throw new ArgumentException($"Destination path {file.Action.DestinationPath} is not directory path.");
            }
            file.TempPath = _fileStreamerWrapper.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
        }
        else
        {
            throw new ArgumentException("No zip file is sent");
        }
    }

    private void HandleFirstMessageAsync(FileDownload file, DownloadBlobChunkMessage message)
    {
        file.TotalBytes = message.FileSize;
        _strictModeHandler.CheckSizeStrictMode(TwinActionType.SingularDownload, file.TotalBytes, file.Action.DestinationPath);
        file.Stopwatch.Start();
    }

    private async Task HandleCompletedDownload(FileDownload file, CancellationToken cancellationToken)
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

    private async Task HandleEndRangeDownload(string filePath, DownloadBlobChunkMessage message, FileDownload file, CancellationToken cancellationToken)
    {
        var isRangeValid = await VerifyRangeCheckSum(filePath, (long)message.RangeStartPosition, (long)message.RangeEndPosition, message.RangeCheckSum);
        if (!isRangeValid)
        {
            await _d2CMessengerHandler.SendFirmwareUpdateEventAsync(cancellationToken, message.FileName, message.ActionId, 0, message.RangeStartPosition, message.RangeEndPosition);
            file.TotalBytesDownloaded -= (long)(message.RangeEndPosition - message.RangeStartPosition);
            if (file.TotalBytesDownloaded < 0) { file.TotalBytesDownloaded = 0; }
        }
        else
        {
            file.Report.CompleteRanges = AddRange(file.Report.CompleteRanges, message.RangeIndex);
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
                await HandleEndRangeDownload(filePath, message, file, cancellationToken);
            }
            if (file.Report.CompleteRanges == (message.RangesCount == 1 ? "0" : $"0-{message.RangesCount - 1}"))
            {
                await HandleCompletedDownload(file, cancellationToken);
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
        return Math.Max((float)progressPercent, 100);
    }

    private async Task<bool> VerifyRangeCheckSum(string filePath, long startPosition, long endPosition, string checkSum)
    {
        long lengthToRead = endPosition - startPosition;
        byte[] data = new byte[lengthToRead];
        using (Stream stream = _fileStreamerWrapper.CreateStream(filePath, FileMode.Open, FileAccess.Read))
        {
            stream.Seek(startPosition, SeekOrigin.Begin);
            stream.Read(data, 0, (int)lengthToRead);
        }

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
        while (_filesDownloads.TryTake(out var removedItem))
        {
            if (removedItem.Action.Source != fileName || removedItem.Action.ActionId != actionId)
            {
                _filesDownloads.Add(removedItem);
            }
        }
    }
}
