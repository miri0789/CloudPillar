using System.Diagnostics;
using CloudPillar.Agent.Interfaces;
using shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;

public class FileDownloadHandler : IFileDownloadHandler
{
    private readonly IFileStreamerFactory _FileStreamerFactory;
    private readonly ID2CEventHandler _D2CEventHandler;
    private IList<FileDownload> _filesDownloads;

    public FileDownloadHandler(IFileStreamerFactory FileStreamerFactory, ID2CEventHandler D2CEventHandler)
    {
        ArgumentNullException.ThrowIfNull(FileStreamerFactory);
        ArgumentNullException.ThrowIfNull(D2CEventHandler);
        _FileStreamerFactory = FileStreamerFactory;
        _D2CEventHandler = D2CEventHandler;
        _filesDownloads = new List<FileDownload>();
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

    public async Task DownloadMessageDataAsync(DownloadBlobChunkMessage blobChunk)
    {
        var file = _filesDownloads.FirstOrDefault(item => item.ActionGuid == blobChunk.ActionGuid && item.FileName == blobChunk.FileName);
        if (file == null)
        {
            throw new InvalidOperationException($"There is no active download for message {blobChunk.GetMessageId()}");
        }
        var filePath = Path.Combine(file.Path, file.FileName);
        if (!file.Stopwatch.IsRunning)
        {
            file.Stopwatch.Start();
            file.TotalBytes = blobChunk.FileSize;
        }
        await _FileStreamerFactory.WriteChunkToFileAsync(filePath, blobChunk.Offset, blobChunk.Data);

        CalculateBytesDownloadedPercent(file, blobChunk.Data.Length, blobChunk.Offset);

        if (file.TotalBytesDownloaded == file.TotalBytes)
        {
            file.Stopwatch.Stop();
            //TODO report success
        }
        else
        {
            //TODO report progress
            if (blobChunk.RangeSize != null)
            {
                await CheckFullRangeBytesAsync(blobChunk, filePath);
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
        var isEmptyRangeBytes = await _FileStreamerFactory.HasBytesAsync(filePath, startPosition, endPosition);
        if (!isEmptyRangeBytes)
        {
            await _D2CEventHandler.SendFirmwareUpdateEventAsync(blobChunk.FileName, blobChunk.ActionGuid, startPosition, endPosition);
        }
    }
}
