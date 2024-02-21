using Backend.Infra.Common.Services.Interfaces;
using Backend.Iotlistener.Interfaces;
using Shared.Entities.Messages;

namespace Backend.Iotlistener.Services;

public class FileDownloadService : IFileDownloadService
{
    private readonly ISendQueueMessagesService _sendQueueMessagesService;
    public FileDownloadService(ISendQueueMessagesService sendQueueMessagesService)
    {
        _sendQueueMessagesService = sendQueueMessagesService ?? throw new ArgumentNullException(nameof(sendQueueMessagesService));
    }

    public async Task SendFileDownloadAsync(string deviceId, FileDownloadEvent data)
    {
        string requestUrl = $"blob/SendFileDownloadChunks?deviceId={deviceId}";
        await _sendQueueMessagesService.SendMessageToQueue(requestUrl, data);
    }
}
