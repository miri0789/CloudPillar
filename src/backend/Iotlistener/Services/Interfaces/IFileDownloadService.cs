using Shared.Entities.QueueMessages;

namespace Backend.Iotlistener.Interfaces;

public interface IFileDownloadService
{
    Task SendFileDownloadAsync(string deviceId, FileDownloadMessage data);
}