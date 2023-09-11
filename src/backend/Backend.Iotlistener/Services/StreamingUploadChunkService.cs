using common;
using Backend.Iotlistener.Interfaces;
using Backend.Iotlistener.Models.Enums;
using Shared.Logger;
using Shared.Entities.Events;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http.Headers;

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
            var requests = new List<Task>();
            string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}blob/uploadStream";
            var jsonContent = JsonConvert.SerializeObject(data);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            requests.Add(_httpRequestorService.SendRequest(requestUrl, HttpMethod.Post, content));
            await Task.WhenAll(requests);

        }
        catch (Exception ex)
        {
            _logger.Error($"StreamingUploadChunkService UploadStreamToBlob failed. Message: {ex.Message}");
        }
    }
}