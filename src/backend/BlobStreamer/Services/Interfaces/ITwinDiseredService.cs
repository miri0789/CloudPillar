using Shared.Entities.Twin;

namespace Backend.BlobStreamer.Interfaces;

public interface ITwinDiseredService
{
    Task AddDesiredRecipeAsync(string deviceId,TwinPatchChangeSpec changeSpecKey, DownloadAction downloadAction);
}