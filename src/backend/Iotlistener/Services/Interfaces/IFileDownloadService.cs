using Shared.Entities.Messages;

namespace Backend.Iotlistener.Interfaces;

public interface IFileDownloadService
{
    Task SendFileDownloadAsync(string deviceId, FileUpdateEvent data);
}