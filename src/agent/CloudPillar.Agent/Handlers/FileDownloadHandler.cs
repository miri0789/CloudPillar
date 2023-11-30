using System.Collections.Concurrent;
using System.Diagnostics;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class FileDownloadHandler : IFileDownloadHandler
{
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly IStrictModeHandler _strictModeHandler;
    private readonly ITwinActionsHandler _twinActionsHandler;
    private static readonly ConcurrentBag<FileDownload> _filesDownloads = new ConcurrentBag<FileDownload>();
    private readonly ILoggerHandler _logger;

    public FileDownloadHandler(IFileStreamerWrapper fileStreamerWrapper,
                               ID2CMessengerHandler d2CMessengerHandler,
                               IStrictModeHandler strictModeHandler,
                               ITwinActionsHandler twinActionsHandler,
                               ILoggerHandler loggerHandler)
    {
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _strictModeHandler = strictModeHandler ?? throw new ArgumentNullException(nameof(strictModeHandler));
        _twinActionsHandler = twinActionsHandler ?? throw new ArgumentNullException(nameof(twinActionsHandler));
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
    }

    public async Task InitFileDownloadAsync(DownloadAction downloadAction, ActionToReport actionToReport)
    {
        _filesDownloads.Add(new FileDownload
        {
            DownloadAction = downloadAction,
            Report = actionToReport,
            Stopwatch = new Stopwatch()
        });
        await _d2CMessengerHandler.SendFirmwareUpdateEventAsync(downloadAction.Source, downloadAction.ActionId);
    }

    public async Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message, CancellationToken cancellationToken)
    {

        var file = _filesDownloads.FirstOrDefault(item => item.DownloadAction.ActionId == message.ActionId &&
                                    item.DownloadAction.Source == message.FileName);
        if (file == null)
        {
            throw new ArgumentException($"There is no active download for message {message.GetMessageId()}");
        }

        if (!file.Stopwatch.IsRunning)
        {
            if (string.IsNullOrEmpty(file.DownloadAction.DestinationPath))
            {
                file.Report.TwinReport.Status = StatusType.Failed;
                file.Report.TwinReport.ResultCode = "Destination path does not exist.";
                return file.Report;
            }
            if (file.DownloadAction.Unzip)
            {
                if (_fileStreamerWrapper.GetExtension(file.DownloadAction.Source).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    var extention = _fileStreamerWrapper.GetExtension(file.DownloadAction.DestinationPath);
                    if (!string.IsNullOrEmpty(extention))
                    {
                        file.Report.TwinReport.Status = StatusType.Failed;
                        file.Report.TwinReport.ResultCode = $"Destination path {file.DownloadAction.DestinationPath} is not directory path.";
                        return file.Report;
                    }
                    file.TempPath = _fileStreamerWrapper.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");
                }
                else
                {
                    _logger.Info($"Since no zip file is sent, the unzip command is ignored - {file.DownloadAction.Source}");
                }
            }
            file.Stopwatch.Start();
            file.TotalBytes = message.FileSize;
        }

        var filePath = file.TempPath ?? file.DownloadAction.DestinationPath;
        try
        {
            //strict mode
            filePath = _strictModeHandler.ReplaceRootById(file.DownloadAction.Action.Value, filePath);
            _strictModeHandler.CheckSizeStrictMode(file.DownloadAction.Action.Value, file.TotalBytes, filePath);
        }
        catch (Exception ex)
        {
            file.Report.TwinReport.Status = StatusType.Failed;
            file.Report.TwinReport.ResultCode = ex.Message;
            return file.Report;
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
            file.Report.TwinReport.Status = StatusType.Success;
        }
        else
        {
            
            file.Report.TwinReport.Status = StatusType.InProgress;
        }
        return file.Report;

    }

    private float CalculateBytesDownloadedPercent(FileDownload file, long bytesLength, long offset)
    {
        const double KB = 1024.0;
        file.TotalBytesDownloaded += bytesLength;
        double progressPercent = Math.Round(file.TotalBytesDownloaded / (double)file.TotalBytes * 100, 2);
        double throughput = file.TotalBytesDownloaded / file.Stopwatch.Elapsed.TotalSeconds / KB;
        _logger.Debug($"%{progressPercent:00} @pos: {offset:00000000000} Throughput: {throughput:0.00} KiB/s");
        return (float)progressPercent;
    }

    private async Task VerifyRangeCheckSum() {

    }
}
