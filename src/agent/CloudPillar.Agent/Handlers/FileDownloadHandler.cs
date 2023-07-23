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
    private readonly ID2CEventHandler _D2CEventHandler;
    private readonly ConcurrentBag<FileDownload> _filesDownloads;

    public FileDownloadHandler(IFileStreamerWrapper FileStreamerWrapper, ID2CEventHandler D2CEventHandler)
    {
        ArgumentNullException.ThrowIfNull(FileStreamerWrapper);
        ArgumentNullException.ThrowIfNull(D2CEventHandler);
        _FileStreamerWrapper = FileStreamerWrapper;
        _D2CEventHandler = D2CEventHandler;
        _filesDownloads = new ConcurrentBag<FileDownload>();
    }

    public async Task InitFileDownloadAsync(ActionToReport action)
    {
        if (action.TwinAction is DownloadAction)

        {
            var downloadAction = (DownloadAction)action.TwinAction;
            _filesDownloads.Add(new FileDownload()
            {
                TwinAction = downloadAction,
                TwinPartName=action.TwinPartName,
                TwinReportIndex=action.TwinReportIndex,
                Stopwatch = new Stopwatch()
            });
            await _D2CEventHandler.SendFirmwareUpdateEventAsync(downloadAction.Source, action.TwinAction.ActionGuid);
        }
    }

    public async Task HandleMessageAsync(BaseMessage message)
    {
        if (message is DownloadBlobChunkMessage blobChunk)
        {
            var file = _filesDownloads.FirstOrDefault(item => item.TwinAction.ActionGuid == blobChunk.ActionGuid &&
             item.TwinAction.Source == blobChunk.FileName);
            if (file == null)
            {
                throw new ArgumentException($"There is no active download for message {blobChunk.GetMessageId()}");
            }
            var filePath = Path.Combine(file.TwinAction.DestinationPath, file.TwinAction.Source);
            if (!file.Stopwatch.IsRunning)
            {
                file.Stopwatch.Start();
                file.TotalBytes = blobChunk.FileSize;
            }
            await _FileStreamerWrapper.WriteChunkToFileAsync(filePath, blobChunk.Offset, blobChunk.Data);

            CalculateBytesDownloadedPercent(file, blobChunk.Data.Length, blobChunk.Offset);

            if (file.TotalBytesDownloaded == file.TotalBytes)
            {
                file.Stopwatch.Stop();
                //TODO report success
            }
            else
            {
                //TODO report progress
                if (blobChunk?.RangeSize != null)
                {
                    await CheckFullRangeBytesAsync(blobChunk, filePath);
                }
            }
        }
        else
        {
            Console.WriteLine($"DownloadBlobChunkMessage HandlMessage message is not in suitable type");
        }

    }

    private void CalculateBytesDownloadedPercent(FileDownload file, long bytesLength, long offset)
    {
        file.TotalBytesDownloaded += bytesLength;
        double progressPercent = Math.Round((double)file.TotalBytesDownloaded / bytesLength * 100, 2);
        double throughput = file.TotalBytesDownloaded / file.Stopwatch.Elapsed.TotalSeconds / 1024.0; // in KiB/s
        Console.WriteLine($"%{progressPercent:00} @pos: {offset:00000000000} Throughput: {throughput:0.00} KiB/s");
        //TODO report percent
    }

    private async Task CheckFullRangeBytesAsync(DownloadBlobChunkMessage blobChunk, string filePath)
    {
        long endPosition = blobChunk.Offset + blobChunk.Data.Length;
        long startPosition = endPosition - (long)blobChunk.RangeSize;
        var isEmptyRangeBytes = await _FileStreamerWrapper.HasBytesAsync(filePath, startPosition, endPosition);
        if (!isEmptyRangeBytes)
        {
            await _D2CEventHandler.SendFirmwareUpdateEventAsync(blobChunk.FileName, blobChunk.ActionGuid, startPosition, endPosition);
        }
    }
}
