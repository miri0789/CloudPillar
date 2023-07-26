using System.Collections.Concurrent;
using System.Diagnostics;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public class FileDownloadHandler : IFileDownloadHandler
{
    private readonly IFileStreamerWrapper _FileStreamerWrapper;
    private readonly ID2CEventHandler _d2CEventHandler;
    private readonly ConcurrentBag<FileDownload> _filesDownloads;

    public FileDownloadHandler(IFileStreamerWrapper FileStreamerWrapper,
                               ID2CEventHandler d2CEventHandler)
    {
        ArgumentNullException.ThrowIfNull(FileStreamerWrapper);
        ArgumentNullException.ThrowIfNull(d2CEventHandler);

        _FileStreamerWrapper = FileStreamerWrapper;
        _d2CEventHandler = d2CEventHandler;
        _filesDownloads = new ConcurrentBag<FileDownload>();
    }

    public async Task InitFileDownloadAsync(ActionToReport action)
    {
        if (action.TwinAction is DownloadAction downloadAction)
        {
            _filesDownloads.Add(new FileDownload()
            {
                TwinAction = downloadAction,
                TwinPartName = action.TwinPartName,
                TwinReportIndex = action.TwinReportIndex,
                Stopwatch = new Stopwatch()
            });
            await _d2CEventHandler.SendFirmwareUpdateEventAsync(downloadAction.Source, action.TwinAction.ActionGuid);
        }
        else
        {
            Console.WriteLine($"InitFileDownloadAsync, no download action is recived, twin part {action.TwinPartName} index {action.TwinReportIndex}");
        }
    }

    public async Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message)
    {

        var file = _filesDownloads.FirstOrDefault(item => item.TwinAction.ActionGuid == message.ActionGuid &&
         item.TwinAction.Source == message.FileName);
        if (file == null)
        {
            throw new ArgumentException($"There is no active download for message {message.GetMessageId()}");
        }
        var filePath = Path.Combine(file.TwinAction.DestinationPath, file.TwinAction.Source);
        if (!file.Stopwatch.IsRunning)
        {
            file.Stopwatch.Start();
            file.TotalBytes = message.FileSize;
        }
        await _FileStreamerWrapper.WriteChunkToFileAsync(filePath, message.Offset, message.Data);

        file.Progress = CalculateBytesDownloadedPercent(file, message.Data.Length, message.Offset);

        if (file.TotalBytesDownloaded == file.TotalBytes)
        {
            file.Stopwatch.Stop();
            file.Status = StatusType.Success;
        }
        else
        {
            if (message?.RangeSize != null)
            {
                await CheckFullRangeBytesAsync(message, filePath);
            }
            file.Status = StatusType.InProgress;
        }
        return file;

    }

    private float CalculateBytesDownloadedPercent(FileDownload file, long bytesLength, long offset)
    {
        const double KB = 1024.0;
        file.TotalBytesDownloaded += bytesLength;
        double progressPercent = Math.Round((double)file.TotalBytesDownloaded / bytesLength * 100, 2);
        double throughput = file.TotalBytesDownloaded / file.Stopwatch.Elapsed.TotalSeconds / KB;
        Console.WriteLine($"%{progressPercent:00} @pos: {offset:00000000000} Throughput: {throughput:0.00} KiB/s");
        return (float)progressPercent;
    }

    private async Task CheckFullRangeBytesAsync(DownloadBlobChunkMessage blobChunk, string filePath)
    {
        long endPosition = blobChunk.Offset + blobChunk.Data.Length;
        long startPosition = endPosition - (long)blobChunk.RangeSize;
        var isEmptyRangeBytes = await _FileStreamerWrapper.HasBytesAsync(filePath, startPosition, endPosition);
        if (!isEmptyRangeBytes)
        {
            await _d2CEventHandler.SendFirmwareUpdateEventAsync(blobChunk.FileName, blobChunk.ActionGuid, startPosition, endPosition);
        }
    }
}
