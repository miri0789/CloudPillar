using System;
using System.Diagnostics;
using shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;

public interface IFileDownloadHandler
{
    void DownloadMessageData(DownloadBlobChunkMessage downloadBlobChunkMessage, byte[] messageData);
}

public class FileDownloadHandler : IFileDownloadHandler
{
    private readonly IFileStreamerHandler _fileStreamerHandler;
    private List<FileDownload> _filesDownloads = new List<FileDownload>();

    public FileDownloadHandler(IFileStreamerHandler fileStreamerHandler)
    {
        _fileStreamerHandler = fileStreamerHandler;
    }

    public void InitFileDownload(Guid actionGuid, string path, string fileName)
    {
        _filesDownloads.Add(new FileDownload()
        {
            ActionGuid = actionGuid,
            Path = path ?? Directory.GetCurrentDirectory(),
            FileName = fileName,
            Stopwatch = new Stopwatch()
        });
    }

    public async void DownloadMessageData(DownloadBlobChunkMessage blobChunk, byte[] fileBytes)
    {
        var file = _filesDownloads.Find(item => item.ActionGuid == blobChunk.ActionGuid && item.FileName == blobChunk.FileName);
        if (file == null)
        {
            throw new Exception($"There is no active download for message {blobChunk.GetMessageId()}");
        }
        var filePath = Path.Combine(file.Path, file.FileName);
        if (!file.Stopwatch.IsRunning)
        {
            file.Stopwatch.Start();
            file.TotalBytes = blobChunk.FileSize;
        }
        await _fileStreamerHandler.WriteChunkToFile(filePath, blobChunk.Offset, fileBytes);

        file.TotalBytesDownloaded += fileBytes.Length;
        double progressPercent = Math.Round((double)file.TotalBytesDownloaded / fileBytes.Length * 100, 2);
        double throughput = file.TotalBytesDownloaded / file.Stopwatch.Elapsed.TotalSeconds / 1024.0; // in KiB/s
        Console.WriteLine($"%{progressPercent:00} @pos: {blobChunk.Offset:00000000000} Throughput: {throughput:0.00} KiB/s");

        if (file.TotalBytesDownloaded == file.TotalBytes)
        {
            //TODO report success
        }
        else
        {
            //TODO report progress
            if (blobChunk.RangeSize != null && blobChunk.RangeSize != 0)
            {
                await CheckFullRangeBytes(blobChunk, filePath, fileBytes.Length);
            }
        }

    }

    private async Task CheckFullRangeBytes(DownloadBlobChunkMessage blobChunk, string filePath, int fileBytesLength)
    {
        long endPosition = blobChunk.Offset + fileBytesLength;
        long startPosition = endPosition - blobChunk.RangeSize;
        var isEmptyRangeBytes = await _fileStreamerHandler.CheckFileBytesNotEmpty(filePath, startPosition, endPosition);
        if (!isEmptyRangeBytes)
        {
            await _fileStreamerHandler.DeleteFileBytes(filePath, startPosition, endPosition);
            //TODO send FirmwareUpdateReady event for this range
        }
    }
}
