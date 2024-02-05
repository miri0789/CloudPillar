using Shared.Entities.Twin;

namespace Backend.Infra.Common.Services.Interfaces;

public interface ITwinDiseredService
{
    Task AddDesiredRecipeAsync(string deviceId, string changeSpecKey, DownloadAction downloadAction, string transactionsKey = TwinConstants.DEFAULT_TRANSACTIONS_KEY);
    Task<TwinDesired> AddChangeSpec(string deviceId, string changeSpecKey, TwinChangeSpec assignChangeSpec, string signature);
    Byte[] GetChangeSpecDataToSign(TwinChangeSpec changeSpec);
    Task SignTwinDesiredAsync(TwinDesired twinDesired, string deviceId, string changeSpecKey, string signData);
    Task<TwinDesired> GetTwinDesiredAsync(string deviceId);
}