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
    private readonly ConcurrentBag<FileDownload> _filesDownloads;
    private readonly ILoggerHandler _logger;

    public FileDownloadHandler(IFileStreamerWrapper fileStreamerWrapper,
                               ID2CMessengerHandler d2CMessengerHandler,
                               ILoggerHandler loggerHandler)
    {
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _filesDownloads = new ConcurrentBag<FileDownload>();
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

    public async Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message)
    {

        var file = _filesDownloads.FirstOrDefault(item => item.DownloadAction.ActionId == message.ActionId &&
                                    item.DownloadAction.Source == message.FileName);
        if (file == null)
        {
            throw new ArgumentException($"There is no active download for message {message.GetMessageId()}");
        }
        var filePath = Path.Combine(file.DownloadAction.DestinationPath, file.DownloadAction.Source);
        if (!file.Stopwatch.IsRunning)
        {
            file.Stopwatch.Start();
            file.TotalBytes = message.FileSize;
        }
        await _fileStreamerWrapper.WriteChunkToFileAsync(filePath, message.Offset, message.Data);
        file.Report.TwinReport.Progress = CalculateBytesDownloadedPercent(file, message.Data.Length, message.Offset);

        if (file.TotalBytesDownloaded == file.TotalBytes)
        {
            file.Stopwatch.Stop();
            file.Report.TwinReport.Status = StatusType.Success;
        }
        else
        {
            if (message?.RangeSize != null)
            {
                // TODO find true way to calculate it
                //  await CheckFullRangeBytesAsync(message, filePath);
            }
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

    private async Task CheckFullRangeBytesAsync(DownloadBlobChunkMessage blobChunk, string filePath)
    {
        long endPosition = blobChunk.Offset + blobChunk.Data.Length;
        long startPosition = endPosition - (long)blobChunk.RangeSize;
        var isEmptyRangeBytes = await _fileStreamerWrapper.HasBytesAsync(filePath, startPosition, endPosition);
        if (!isEmptyRangeBytes)
        {
            await _d2CMessengerHandler.SendFirmwareUpdateEventAsync(blobChunk.FileName, blobChunk.ActionId, startPosition, endPosition);
        }
    }
}
