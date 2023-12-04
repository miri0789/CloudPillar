using Backend.Infra.Common.Services.Interfaces;
using Backend.Iotlistener.Interfaces;
using Shared.Logger;
using Shared.Entities.Messages;
using Backend.Infra.Common;

namespace Backend.Iotlistener.Services;

public class StreamingUploadChunkService : IStreamingUploadChunkService
{
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;

    public StreamingUploadChunkService(IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper,
     ILoggerHandler logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
    }

    public async Task UploadStreamToBlob(StreamingUploadChunkEvent data, string deviceId)
    {
        long chunkIndex = (data.StartPosition / data.Data.Length) + 1;

        _logger.Info($"IotListener: Send chunk number {chunkIndex} to BlobStreamer");

        string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/uploadStream?deviceId={deviceId}";
        await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post, data);
    }

    public async Task DeleteBlob(DeleteBlobEvent data)
    {
        _logger.Info($"IotListener: Delete blob {data.StorageUri} from BlobStreamer");
        string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/deleteBlob";
        await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post, data);
    }
}