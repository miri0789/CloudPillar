using Shared.Entities.Twin;

namespace Backend.Infra.Common.Services.Interfaces;

public interface ITwinDiseredService
{
    Task AddDesiredRecipeAsync(string deviceId, string changeSpecKey, DownloadAction downloadAction, string transactionsKey = TwinConstants.DEFAULT_TRANSACTIONS_KEY);
    Task<TwinDesired> AddChangeSpec(string deviceId, AssignChangeSpec assignChangeSpec);
    Byte[] GetTwinDesiredDataToSign(TwinDesired twinDesired, string changeSpecKey);
    Task SignTwinDesiredAsync(TwinDesired twinDesired, string deviceId, string changeSpecKey, string signData);
    Task<TwinDesired> GetTwinDesiredAsync(string deviceId);
}