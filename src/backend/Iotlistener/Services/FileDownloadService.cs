using Backend.Iotlistener.Interfaces;
using Shared.Logger;
using Shared.Entities.Messages;
using Backend.Infra.Common.Services.Interfaces;

namespace Backend.Iotlistener.Services;

public class FileDownloadService : IFileDownloadService
{
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    public FileDownloadService(IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
    }

    public async Task SendFileDownloadAsync(string deviceId, FileDownloadEvent data)
    {
        string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}FileDownloadChunks/SendFileDownloadChunks?deviceId={deviceId}";
        await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post, data);
    }
}