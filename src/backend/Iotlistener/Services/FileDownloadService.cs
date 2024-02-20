using Backend.Iotlistener.Interfaces;
using Shared.Logger;
using Backend.Infra.Common.Services.Interfaces;
using Newtonsoft.Json;
using Shared.Entities.QueueMessages;

namespace Backend.Iotlistener.Services;

public class FileDownloadService : IFileDownloadService
{
    private readonly ISendQueueMessagesService _queueMessagesService;
    private readonly ILoggerHandler _logger;

    public FileDownloadService(ISendQueueMessagesService sendQueueMessage, ILoggerHandler logger)
    {
        _queueMessagesService = sendQueueMessage ?? throw new ArgumentNullException(nameof(sendQueueMessage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendFileDownloadAsync(FileDownloadQueueMessage data)
    {
        _logger.Info("Sending file download message to the queue");
        var message = JsonConvert.SerializeObject(data);
        await _queueMessagesService.SendMessageToQueue(message);
    }
}