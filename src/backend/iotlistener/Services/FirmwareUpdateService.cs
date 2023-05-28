using common;
using Microsoft.Azure.Storage.Blob;

namespace iotlistener;

public interface IFirmwareUpdateService
{
    Task SendFirmwareUpdateAsync(string deviceId, FirmwareUpdateEvent data);
}

public class FirmwareUpdateService: IFirmwareUpdateService
{

    private readonly IHttpRequestorService _httpRequestorService;
    private readonly string _blobStreamerUrl;
    public FirmwareUpdateService(IHttpRequestorService httpRequestorService)
    {
        _httpRequestorService = httpRequestorService;
        _blobStreamerUrl = Environment.GetEnvironmentVariable(Constants.blobStreamerUrl)!;
    }

    public async Task SendFirmwareUpdateAsync(string deviceId, FirmwareUpdateEvent data)
    {
        long blobSize = await GetBlobSize(data.fileName);
        long rangeSize = getRangeSize(blobSize, data.chunkSize);

        string startRangeRequestUrl = $"{_blobStreamerUrl}start?deviceId={deviceId}&fileName={data.fileName}&blobLength={blobSize}";
        await _httpRequestorService.SendRequest<object>(startRangeRequestUrl, HttpMethod.Post);

        var requests = new List<Task>();
        for (long offset = data.startPosition, rangeIndex = 0; offset < blobSize; offset += rangeSize, rangeIndex++)
        {
            string requestUrl = $"{_blobStreamerUrl}range?deviceId={deviceId}&fileName={data.fileName}&chunkSize={data.chunkSize}&rangeSize={rangeSize}&rangeIndex={rangeIndex}&startPosition={offset}";
            requests.Add(_httpRequestorService.SendRequest<object>(requestUrl, HttpMethod.Post));
        }
        await Task.WhenAll(requests);
    }

    private async Task<long> GetBlobSize(string fileName)
    {
        try
        {
            string requestUrl = $"{_blobStreamerUrl}metadata?fileName={fileName}";
            BlobProperties fileMetadata = await _httpRequestorService.SendRequest<BlobProperties>(requestUrl, HttpMethod.Get);
            return fileMetadata.Length;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FirmwareUpdateService GetBlobSize failed. Message: {ex.Message}");
            throw;
        }
    }

    private long getRangeSize(long blobSize, int chunkSize)
    {
        int.TryParse(Environment.GetEnvironmentVariable(Constants.rangePercent), out int rangePercent);
        int.TryParse(Environment.GetEnvironmentVariable(Constants.rangeBytes), out int rangeBytes);
        string rangeCalculateTypeString = Environment.GetEnvironmentVariable(Constants.rangeCalculateType);
        Enum.TryParse(rangeCalculateTypeString, ignoreCase: true, out RangeCalculateType rangeCalculateType);

        if (rangeCalculateType == RangeCalculateType.Bytes && rangeBytes != null)
        {
            return rangeBytes > chunkSize ? rangeBytes : chunkSize;
        }
        else if (rangeCalculateType == RangeCalculateType.Percent && rangePercent != null)
        {
            var rangeSize = blobSize / 100 * rangePercent;
            return rangeSize > chunkSize ? rangeSize : chunkSize;
        }

        return blobSize;
    }
}
