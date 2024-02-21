using Shared.Entities.Messages;

namespace Backend.BEApi.Services.Interfaces;

public interface ILoadTestingService
{
    Task SendFileDownloadAsync(string deviceId, FileDownloadEvent data);

}
