using Microsoft.Azure.Storage.Blob;
using common;

namespace iotlistener;

public interface IFirmwareUpdateService
{
    Task SendFirmwareUpdateAsync(string deviceId, FirmwareUpdateEvent data);
}

public class FirmwareUpdateService : IFirmwareUpdateService
{
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly Uri _blobStreamerUrl;
    public FirmwareUpdateService(IHttpRequestorService httpRequestorService)
    {
        _httpRequestorService = httpRequestorService;
        _blobStreamerUrl = new Uri(Environment.GetEnvironmentVariable(Constants.blobStreamerUrl)!);
    }

    public async Task SendFirmwareUpdateAsync(string deviceId, FirmwareUpdateEvent data)
    {
        try
        {
            long blobSize = await GetBlobSize(data.fileName);
            long rangeSize = getRangeSize(blobSize, data.chunkSize);

            string startRangeRequestUrl = $"{_blobStreamerUrl}blob/start?deviceId={deviceId}&fileName={data.fileName}&blobLength={blobSize}";
            await _httpRequestorService.SendRequest(startRangeRequestUrl, HttpMethod.Post);

            var requests = new List<Task>();
            for (long offset = data.startPosition, rangeIndex = 0; offset < blobSize; offset += rangeSize, rangeIndex++)
            {
                string requestUrl = $"{_blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={data.fileName}&chunkSize={data.chunkSize}&rangeSize={rangeSize}&rangeIndex={rangeIndex}&startPosition={offset}";
                requests.Add(_httpRequestorService.SendRequest(requestUrl, HttpMethod.Post));
            }
            await Task.WhenAll(requests);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FirmwareUpdateService SendFirmwareUpdateAsync failed. Message: {ex.Message}");
        }
    }

    private async Task<long> GetBlobSize(string fileName)
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
