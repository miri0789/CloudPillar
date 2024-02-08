using Backend.Iotlistener.Interfaces;
using Backend.Iotlistener.Models.Enums;
using Shared.Logger;
using Backend.Iotlistener.Models;
using Shared.Entities.Messages;
using Backend.Infra.Common.Services.Interfaces;

namespace Backend.Iotlistener.Services;

public class FileDownloadService : IFileDownloadService
{
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;
    public FileDownloadService(IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper,
     ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
    }

    public async Task SendFileDownloadAsync(string deviceId, FileDownloadEvent data)
    {
        try
        {
            var blobSize = await GetBlobSize(data.FileName);
            ArgumentNullException.ThrowIfNull(blobSize);
            long rangeSize = GetRangeSize((long)blobSize, data.ChunkSize);
            var rangesCount = Math.Ceiling((decimal)blobSize / rangeSize);
            if (data.EndPosition != null)
            {
                rangeSize = (long)data.EndPosition - data.StartPosition;
                string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={data.FileName}&chunkSize={data.ChunkSize}&rangeSize={rangeSize}&rangeIndex={data.CompletedRanges}&startPosition={data.StartPosition}&actionIndex={data.ActionIndex}&rangesCount={rangesCount}&changeSpecId={data.ChangeSpecId}";
                await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post);
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
                            string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={data.FileName}&chunkSize={data.ChunkSize}&rangeSize={rangeSize}&rangeIndex={rangeIndex}&startPosition={offset}&actionIndex={data.ActionIndex}&rangesCount={rangesCount}&changeSpecId={data.ChangeSpecId}";
                            requests.Add(_httpRequestorService.SendRequest<bool>(requestUrl, HttpMethod.Post));
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
            await SendRangeError(deviceId, data.ChangeSpecId, data.FileName, data.ActionIndex, ex.Message);
        }
    }

    private async Task SendRangeError(string deviceId, string changeSpecId, string fileName, int actionIndex, string error)
    {
        try
        {
            string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/rangeError?deviceId={deviceId}&fileName={fileName}&actionIndex={actionIndex}&error={error}&changeSpecId={changeSpecId}";
            await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post);
        }
        catch (Exception ex)
        {
            _logger.Error($"FileDownloadService SendRangeError failed. Message: {ex.Message}");
        }
    }

    private async Task<long?> GetBlobSize(string fileName)
    {
        try
        {
            string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/metadata?fileName={fileName}";
            var fileMetadata = await _httpRequestorService.SendRequest<BlobData>(requestUrl, HttpMethod.Get);
            return fileMetadata.Length;
        }
        catch (Exception ex)
        {
            throw ex;
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
