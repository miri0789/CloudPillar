using Backend.Iotlistener.Interfaces;
using Shared.Logger;
using Shared.Entities.Messages;
using Backend.Infra.Common.Services.Interfaces;
using Newtonsoft.Json;
using Shared.Entities.QueueMessages;

namespace Backend.Iotlistener.Services;

public class FileDownloadService : IFileDownloadService
{
    private readonly IHttpRequestorService _httpRequestorService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly IQueueMessagesService _sendQueueMessage;
    private readonly ILoggerHandler _logger;

    public FileDownloadService(IHttpRequestorService httpRequestorService, IEnvironmentsWrapper environmentsWrapper, IQueueMessagesService sendQueueMessage, ILoggerHandler logger)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _httpRequestorService = httpRequestorService ?? throw new ArgumentNullException(nameof(httpRequestorService));
        _sendQueueMessage = sendQueueMessage ?? throw new ArgumentNullException(nameof(sendQueueMessage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendFileDownloadAsync(string deviceId, FileDownloadMessage data)
    {
        string requestUrl = $"{_environmentsWrapper.blobStreamerUrl}FileDownloadChunks/SendFileDownloadChunks?deviceId={deviceId}";
        await _httpRequestorService.SendRequest(requestUrl, HttpMethod.Post, data);
        var message = JsonConvert.SerializeObject(data);
        await _sendQueueMessage.SendMessageToQueue(message);
    }
}