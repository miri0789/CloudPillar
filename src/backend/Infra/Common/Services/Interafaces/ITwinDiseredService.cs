using Shared.Entities.Twin;

namespace Backend.Infra.Common.Services.Interfaces;

public interface ITwinDiseredService
{
    Task AddDesiredRecipeAsync(string deviceId, string changeSpecKey, DownloadAction downloadAction, string transactionsKey = SharedConstants.DEFAULT_TRANSACTIONS_KEY);
}