using Backend.BlobStreamer.Enums;
using Backend.BlobStreamer.Services.Interfaces;
using Backend.BlobStreamer.Wrappers.Interfaces;
using Shared.Entities.Messages;
using Shared.Logger;

namespace Backend.BlobStreamer.Services;

public class FileDownloadChunksService : IFileDownloadChunksService
{
    private readonly ILoggerHandler _logger;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly IBlobService _blobService;

    public FileDownloadChunksService(ILoggerHandler logger, IEnvironmentsWrapper environmentsWrapper, IBlobService blobService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _blobService = blobService ?? throw new ArgumentNullException(nameof(blobService));
    }


    public async Task SendFileDownloadAsync(string deviceId, FileDownloadEvent data)
    {
        ArgumentNullException.ThrowIfNull(data.ChangeSpecId);
        try
        {
            var blobProperties = await _blobService.GetBlobMetadataAsync(data.FileName);
            var blobSize = blobProperties.Length;
            ArgumentNullException.ThrowIfNull(blobSize);

            long rangeSize = GetRangeSize((long)blobSize, data.ChunkSize);
            var rangesCount = Math.Ceiling((decimal)blobSize / rangeSize);
            if (data.EndPosition != null)
            {
                rangeSize = (long)data.EndPosition - data.StartPosition;
                await _blobService.SendRangeByChunksAsync(deviceId, data.ChangeSpecId, data.FileName, data.ChunkSize, (int)rangeSize, int.Parse(data.CompletedRanges), data.StartPosition, data.ActionIndex, (int)rangesCount);
            }
            else
            {
                long offset = data.StartPosition;
                var existRanges = (data.CompletedRanges ?? "").Split(',').ToList();
                var rangeIndex = 0;
                while (offset < blobSize)
                {
                    _logger.Info($"FileDownloadService Send ranges to blob streamer, range index: {rangeIndex}");
                    var requests = new List<Task<bool>>();
                    for (var i = 0; requests.Count < 4 && offset < blobSize; i++, offset += rangeSize, rangeIndex++)
                    {
                        if (existRanges.IndexOf(rangeIndex.ToString()) == -1)
                        {
                            await _blobService.SendRangeByChunksAsync(deviceId, data.ChangeSpecId, data.FileName, data.ChunkSize, (int)rangeSize, rangeIndex, data.StartPosition, data.ActionIndex, (int)rangesCount);
                        }
                    }
                    await Task.WhenAll(requests);
                    if (requests.Any(task => !task.Result))
                    {
                        _logger.Error($"FileDownloadService SendFileDownloadAsync failed to send range.");
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"FileDownloadService SendFileDownloadAsync failed. Message: {ex.Message}");
            await _blobService.SendDownloadErrorAsync(deviceId, data.ChangeSpecId, data.FileName, data.ActionIndex, ex.Message);
        }
    }

    private long GetRangeSize(long blobSize, int chunkSize)
    {

        if (_environmentsWrapper.rangeCalculateType == RangeCalculateType.Bytes && _environmentsWrapper.rangeBytes != 0)
        {
            return _environmentsWrapper.rangeBytes > chunkSize ? _environmentsWrapper.rangeBytes : chunkSize;
        }
        else if (_environmentsWrapper.rangeCalculateType == RangeCalculateType.Percent && _environmentsWrapper.rangePercent != 0)
        {
            var rangeSize = blobSize / 100 * _environmentsWrapper.rangePercent;
            return rangeSize > chunkSize ? rangeSize : chunkSize;
        }

        return blobSize;
    }
}