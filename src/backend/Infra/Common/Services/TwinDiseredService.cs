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

    private const string DIAGNOSTICS_TRANSACTIONS_KEY = "diagnosticsActions";
    private readonly IRegistryManagerWrapper _registryManagerWrapper;
    private readonly ILoggerHandler _logger;
    private readonly IGuidWrapper _guidWrapper;

    public TwinDiseredService(ILoggerHandler logger, IRegistryManagerWrapper registryManagerWrapper, IGuidWrapper guidWrapper)
    {
        _registryManagerWrapper = registryManagerWrapper ?? throw new ArgumentNullException(nameof(registryManagerWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _guidWrapper = guidWrapper ?? throw new ArgumentNullException(nameof(guidWrapper));
    }

    public async Task AddDesiredRecipeAsync(string deviceId, TwinPatchChangeSpec changeSpecKey, DownloadAction downloadAction)
    {
        ArgumentNullException.ThrowIfNull(deviceId);

        try
        {
            using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
            {
                var twin = await _registryManagerWrapper.GetTwinAsync(registryManager, deviceId);
                TwinDesired twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();
                var twinDesiredChangeSpec = twinDesired.GetDesiredChangeSpecByKey(changeSpecKey);

                if (string.IsNullOrEmpty(twinDesiredChangeSpec.Id))
                {
                    twinDesiredChangeSpec.Id = _guidWrapper.NewGuid();
                }
                if (twinDesiredChangeSpec.Patch is null || twinDesiredChangeSpec.Patch.Values.Count() == 0)
                {
                    twinDesiredChangeSpec.Patch = new Dictionary<string, TwinAction[]>
                    {
                        { DIAGNOSTICS_TRANSACTIONS_KEY, new TwinAction[0] }
                    };
                }

                var updatedArray = twinDesiredChangeSpec.Patch[DIAGNOSTICS_TRANSACTIONS_KEY].ToList();
                updatedArray.Add(downloadAction);

                twinDesiredChangeSpec.Patch[DIAGNOSTICS_TRANSACTIONS_KEY] = updatedArray.ToArray();
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