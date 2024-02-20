using Shared.Entities.QueueMessages;

namespace Backend.Iotlistener.Interfaces;

public interface IFileDownloadService
{
    Task SendFileDownloadAsync(FileDownloadQueueMessage data);
}