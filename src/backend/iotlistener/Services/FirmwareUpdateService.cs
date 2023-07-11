using Microsoft.Azure.Storage.Blob;
using common;
using iotlistener.Interfaces;

namespace iotlistener.Services;

public class FirmwareUpdateService : IFirmwareUpdateService
{
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly Uri _blobStreamerUrl;
    private readonly int _rangePercent;
    private readonly int _rangeBytes;
    private readonly RangeCalculateType _rangeCalculateType;
    public FirmwareUpdateService(IHttpRequestorService httpRequestorService)
    {
        ArgumentNullException.ThrowIfNull(httpRequestorService);
        _httpRequestorService = httpRequestorService;
        _blobStreamerUrl = new Uri(Environment.GetEnvironmentVariable(Constants.blobStreamerUrl)!);
        int.TryParse(Environment.GetEnvironmentVariable(Constants.rangePercent), out _rangePercent);
        int.TryParse(Environment.GetEnvironmentVariable(Constants.rangeBytes), out _rangeBytes);
        Enum.TryParse(Environment.GetEnvironmentVariable(Constants.rangeCalculateType), ignoreCase: true, out RangeCalculateType rangeCalculateType);
    }

    public async Task SendFirmwareUpdateAsync(string deviceId, FirmwareUpdateEvent data)
    {

        long? blobSize = await GetBlobSize(data.FileName);
        if (blobSize != null)
        {
            try
            {
                long rangeSize = GetRangeSize((long)blobSize, data.ChunkSize);

                var requests = new List<Task>();

                if (data.EndPosition != null)
                {
                    rangeSize = (long)data.EndPosition - data.StartPosition;
                    string requestUrl = $"{_blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={data.FileName}&chunkSize={data.ChunkSize}&rangeSize={rangeSize}&rangeIndex=0&startPosition={data.StartPosition}&actionGuid={data.ActionGuid}&fileSize={blobSize}";
                    requests.Add(_httpRequestorService.SendRequest(requestUrl, HttpMethod.Post));
                }
                else
                {
                    for (long offset = data.StartPosition, rangeIndex = 0; offset < blobSize; offset += rangeSize, rangeIndex++)
                    {
                        string requestUrl = $"{_blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={data.FileName}&chunkSize={data.ChunkSize}&rangeSize={rangeSize}&rangeIndex={rangeIndex}&startPosition={offset}&actionGuid={data.ActionGuid}&fileSize={blobSize}";
                        requests.Add(_httpRequestorService.SendRequest(requestUrl, HttpMethod.Post));
                    }
                }
                await Task.WhenAll(requests);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FirmwareUpdateService SendFirmwareUpdateAsync failed. Message: {ex.Message}");

            }
        }
    }

    private async Task<long?> GetBlobSize(string fileName)
    {
        try
        {
            string requestUrl = $"{_blobStreamerUrl}blob/metadata?fileName={fileName}";
            BlobProperties fileMetadata = await _httpRequestorService.SendRequest<BlobProperties>(requestUrl, HttpMethod.Get);
            return fileMetadata.Length;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FirmwareUpdateService GetBlobSize failed. Message: {ex.Message}");
            return null;
        }
    }

    private long GetRangeSize(long blobSize, int chunkSize)
    {

        if (_rangeCalculateType == RangeCalculateType.Bytes && _rangeBytes != 0)
        {
            return _rangeBytes > chunkSize ? _rangeBytes : chunkSize;
        }
        else if (_rangeCalculateType == RangeCalculateType.Percent && _rangePercent != 0)
        {
            var rangeSize = blobSize / 100 * _rangePercent;
            return rangeSize > chunkSize ? rangeSize : chunkSize;
        }

        return blobSize;
    }
}
