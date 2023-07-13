using Microsoft.Azure.Storage.Blob;
using common;
using Backend.Iotlistener.Interfaces;
using Backend.Iotlistener.Models.Enums;

namespace Backend.Iotlistener.Services;

public class FirmwareUpdateService : IFirmwareUpdateService
{
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    public FirmwareUpdateService(IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper)
    {
        ArgumentNullException.ThrowIfNull(httpRequestorService);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);

        _environmentsWrapper = environmentsWrapper;
        _httpRequestorService = httpRequestorService;
        
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
                    string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={data.FileName}&chunkSize={data.ChunkSize}&rangeSize={rangeSize}&rangeIndex=0&startPosition={data.StartPosition}&actionGuid={data.ActionGuid}&fileSize={blobSize}";
                    requests.Add(_httpRequestorService.SendRequest(requestUrl, HttpMethod.Post));
                }
                else
                {
                    for (long offset = data.StartPosition, rangeIndex = 0; offset < blobSize; offset += rangeSize, rangeIndex++)
                    {
                        string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={data.FileName}&chunkSize={data.ChunkSize}&rangeSize={rangeSize}&rangeIndex={rangeIndex}&startPosition={offset}&actionGuid={data.ActionGuid}&fileSize={blobSize}";
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
            string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/metadata?fileName={fileName}";
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
