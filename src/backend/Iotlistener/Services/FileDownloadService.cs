using Backend.Iotlistener.Interfaces;
using Shared.Entities.Messages;
using Backend.Infra.Common.Services.Interfaces;

namespace Backend.Iotlistener.Services;

public class FileDownloadService : IFileDownloadService
{
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ISendQueueMessagesService _sendQueueMessagesService;
    public FileDownloadService(IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper, ISendQueueMessagesService sendQueueMessagesService)
    {
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _sendQueueMessagesService = sendQueueMessagesService ?? throw new ArgumentNullException(nameof(sendQueueMessagesService));
    }

    public async Task SendFileDownloadAsync(string deviceId, FileDownloadEvent data)
    {
        string requestUrl = $"blob/SendFileDownloadChunks?deviceId={deviceId}";
        await _sendQueueMessagesService.SendMessageToQueue(requestUrl, data);
    }
}