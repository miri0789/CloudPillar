using Shared.Entities.Twin;

namespace Backend.BlobStreamer.Interfaces;

public interface ITwinDiseredService
{
        Task AddDesiredToTwin(string deviceId, DownloadAction downloadAction);
}