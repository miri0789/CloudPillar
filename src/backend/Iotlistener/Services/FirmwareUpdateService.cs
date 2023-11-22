using Microsoft.Azure.Storage.Blob;
using Backend.Infra.Common;
using Backend.Iotlistener.Interfaces;
using Backend.Iotlistener.Models.Enums;
using Shared.Logger;
using Backend.Iotlistener.Models;
using Shared.Entities.Messages;

namespace Backend.Iotlistener.Services;

public class FirmwareUpdateService : IFirmwareUpdateService
{
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;
    public FirmwareUpdateService(IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper,
     ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
    }

    public async Task SendFirmwareUpdateAsync(string deviceId, FirmwareUpdateEvent data)
    {

        long? blobSize = await GetBlobSize(data.FileName);
        if (blobSize != null)
        {
            var semaphore = new SemaphoreSlim(4);
            try
            {
                long rangeSize = GetRangeSize((long)blobSize, data.ChunkSize);

                var requests = new List<Task>();

                async Task SendRequestAsync(long offset, long rangeIndex)
                {
                    string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={data.FileName}&chunkSize={data.ChunkSize}&rangeSize={rangeSize}&rangeIndex={rangeIndex}&startPosition={offset}&actionId={data.ActionId}&fileSize={blobSize}";

                    // Use the semaphore to limit the number of parallel requests
                    await semaphore.WaitAsync();
                    try
                    {
                        requests.Add(_httpRequestorService.SendRequest(requestUrl, HttpMethod.Post));
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }

                if (data.EndPosition != null)
                {
                    rangeSize = (long)data.EndPosition - data.StartPosition;
                    await SendRequestAsync(data.StartPosition, 0);
                }
                else
                {
                    for (long offset = data.StartPosition, rangeIndex = 0; offset < blobSize; offset += rangeSize, rangeIndex++)
                    {
                        await SendRequestAsync(offset, rangeIndex);
                    }
                }

                await Task.WhenAll(requests);
            }
            catch (Exception ex)
            {
                _logger.Error($"FirmwareUpdateService SendFirmwareUpdateAsync failed. Message: {ex.Message}");
            }
            finally
            {
                semaphore.Release();
            }
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
            _logger.Error($"FirmwareUpdateService GetBlobSize failed. Message: {ex.Message}");
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
