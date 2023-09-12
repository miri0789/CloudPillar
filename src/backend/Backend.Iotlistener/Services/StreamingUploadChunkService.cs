using common;
using Backend.Iotlistener.Interfaces;
using Shared.Logger;
using Shared.Entities.Events;

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

    public async Task UploadStreamToBlob(StreamingUploadChunkEvent data)
    {
        try
        {
            _logger.Info($"IotListener: Send chunk number {data.ChunkIndex} to BlobStreamer");

            string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/uploadStream";
            await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post, data);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}