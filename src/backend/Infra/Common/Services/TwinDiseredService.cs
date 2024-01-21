using Microsoft.Azure.Devices.Shared;

using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;

using Newtonsoft.Json;

using Shared.Entities.Twin;
using Shared.Entities.Utilities;
using Shared.Logger;

namespace Backend.Infra.Common.Services;

public class TwinDiseredService : ITwinDiseredService
{
    private readonly IRegistryManagerWrapper _registryManagerWrapper;
    private readonly ILoggerHandler _logger;

    public TwinDiseredService(ILoggerHandler logger, IRegistryManagerWrapper registryManagerWrapper)
    {
        _registryManagerWrapper = registryManagerWrapper ?? throw new ArgumentNullException(nameof(registryManagerWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }

    public async Task AddDesiredRecipeAsync(string deviceId, string changeSpecKey, DownloadAction downloadAction, string transactionsKey = TwinConstants.DEFAULT_TRANSACTIONS_KEY)
    {
        ArgumentNullException.ThrowIfNull(deviceId);

        try
        {
            using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
            {
                var twin = await _registryManagerWrapper.GetTwinAsync(registryManager, deviceId);
                TwinDesired twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();
                var twinDesiredChangeSpec = twinDesired.GetDesiredChangeSpecByKey(changeSpecKey);


                twinDesired.ChangeSpec ??= new Dictionary<string, TwinChangeSpec>();
                if (twinDesiredChangeSpec is null)
                {
                    twinDesiredChangeSpec = new TwinChangeSpec() { Patch = new Dictionary<string, TwinAction[]>() };
                    twinDesired.ChangeSpec.Add(changeSpecKey, twinDesiredChangeSpec);
                }

                if (string.IsNullOrEmpty(twinDesiredChangeSpec.Id))
                {
                    twinDesiredChangeSpec.Id = $"{changeSpecKey}-{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}";
                }

                if (twinDesiredChangeSpec?.Patch is null || twinDesiredChangeSpec?.Patch.Values.Count() == 0)
                {
                    twinDesiredChangeSpec.Patch = new Dictionary<string, TwinAction[]>
                    {
                        { transactionsKey, new TwinAction[0] }
                    };
                }

                var updatedArray = twinDesiredChangeSpec.Patch[transactionsKey].ToList();
                updatedArray.Add(downloadAction);

                twinDesiredChangeSpec.Patch[transactionsKey] = updatedArray.ToArray();
                var twinDesiredJson = JsonConvert.SerializeObject(twinDesired.ConvertToJObject());
                twin.Properties.Desired = new TwinCollection(twinDesiredJson);

                await _registryManagerWrapper.UpdateTwinAsync(registryManager, deviceId, twin, twin.ETag);
                _logger.Info($"A new recipe has been successfully added to {deviceId} ");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while attempting to update ChangeSpecDiagnostics: {ex.Message}");
        }

    }
}