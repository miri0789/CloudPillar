using Shared.Entities.Twin;

namespace Backend.Infra.Common.Services.Interfaces;

public interface ITwinDiseredService
{
    Task AddDesiredRecipeAsync(string deviceId, TwinPatchChangeSpec changeSpecKey, DownloadAction downloadAction);
    Task ChangeDesiredRecipeAsync(string deviceId, TwinPatchChangeSpec changeSpecKey, string actionId, string signature);
}