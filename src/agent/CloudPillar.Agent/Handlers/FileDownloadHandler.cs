using System.Collections.Concurrent;
using System.Diagnostics;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Messages;

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

    public async Task InitFileDownloadAsync(Guid actionGuid, string path, string fileName)
    {
        _filesDownloads.Add(new FileDownload()
        {
            ActionGuid = actionGuid,
            Path = path ?? Directory.GetCurrentDirectory(),
            FileName = fileName,
            Stopwatch = new Stopwatch()
        });
        await _D2CEventHandler.SendFirmwareUpdateEventAsync(fileName, actionGuid);
    }

    public async Task HandleDownloadMessageAsync(DownloadBlobChunkMessage message)
    {
        var file = _filesDownloads.FirstOrDefault(item => item.ActionGuid == message.ActionGuid && item.FileName == message.FileName);
        if (file == null)
        {
            throw new ArgumentException($"There is no active download for message {message.GetMessageId()}");
        }
        var filePath = Path.Combine(file.Path, file.FileName);
        if (!file.Stopwatch.IsRunning)
        {
            file.Stopwatch.Start();
            file.TotalBytes = message.FileSize;
        }
        await _FileStreamerWrapper.WriteChunkToFileAsync(filePath, message.Offset, message.Data);

        CalculateBytesDownloadedPercent(file, message.Data.Length, message.Offset);

        if (file.TotalBytesDownloaded == file.TotalBytes)
        {
            file.Stopwatch.Stop();
            //TODO report success
        }
        else
        {
            //TODO report progress
            if (message?.RangeSize != null)
            {
                await CheckFullRangeBytesAsync(message, filePath);
            }
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
