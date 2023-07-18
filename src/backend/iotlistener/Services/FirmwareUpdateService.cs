﻿using System.Security.Cryptography;
using System;
using System.Xml.Schema;
using Microsoft.Azure.Storage.Blob;
using common;
using shared.Entities;
using Shared.Logger;

namespace iotlistener;

public interface IFirmwareUpdateService
{
    Task SendFirmwareUpdateAsync(string deviceId, FirmwareUpdateEvent data);
}

public class FirmwareUpdateService : IFirmwareUpdateService
{
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly Uri _blobStreamerUrl;
    private ILoggerHandler _logger;
    public FirmwareUpdateService(IHttpRequestorService httpRequestorService, ILoggerHandler logger)
    {
        _httpRequestorService = httpRequestorService;
        _blobStreamerUrl = new Uri(Environment.GetEnvironmentVariable(Constants.blobStreamerUrl)!);

        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task SendFirmwareUpdateAsync(string deviceId, FirmwareUpdateEvent data)
    {
        try
        {
            long blobSize = await GetBlobSize(data.FileName);
            long rangeSize = getRangeSize(blobSize, data.ChunkSize);

            var requests = new List<Task>();
            for (long offset = data.StartPosition, rangeIndex = 0; offset < blobSize; offset += rangeSize, rangeIndex++)
            {
                string requestUrl = $"{_blobStreamerUrl}blob/range?deviceId={deviceId}&fileName={data.FileName}&chunkSize={data.ChunkSize}&rangeSize={rangeSize}&rangeIndex={rangeIndex}&startPosition={offset}";
                requests.Add(_httpRequestorService.SendRequest(requestUrl, HttpMethod.Post));
            }
            await Task.WhenAll(requests);
        }
        catch (Exception ex)
        {
            _logger.Error($"FirmwareUpdateService SendFirmwareUpdateAsync failed.", ex);
            throw ex;
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
            _logger.Error($"FirmwareUpdateService GetBlobSize failed.", ex);
            throw ex;
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
