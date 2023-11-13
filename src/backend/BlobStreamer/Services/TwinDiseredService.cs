using Backend.BlobStreamer.Interfaces;
using Shared.Logger;
using Backend.Infra.Common;
using Microsoft.Azure.Devices.Shared;
using Shared.Entities.Twin;
using Newtonsoft.Json;
using Shared.Entities.Utilities;

namespace Backend.BlobStreamer.Services;

public class TwinDiseredService : ITwinDiseredService
{
    private readonly IRegistryManagerWrapper _registryManagerWrapper;
    private readonly ILoggerHandler _logger;

    public TwinDiseredService(ILoggerHandler logger, IRegistryManagerWrapper registryManagerWrapper)
    {
        _registryManagerWrapper = registryManagerWrapper ?? throw new ArgumentNullException(nameof(registryManagerWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    }

    public async Task AddDesiredRecipeAsync(string deviceId,TwinPatchChangeSpec changeSpecKey, DownloadAction downloadAction)
    {
        try
        {
            var twin = await _registryManagerWrapper.GetTwinAsync(deviceId);
            TwinDesired twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();
            var twinDesiredChangeSpec = twinDesired.GetDesiredChangeSpecByKey(changeSpecKey);

            TwinAction[] changeSpecData = twinDesiredChangeSpec.Patch.TransitPackage as TwinAction[] ?? new TwinAction[0];

            var updatedArray = new List<TwinAction>(changeSpecData);
            updatedArray.Add(downloadAction);

            twinDesiredChangeSpec.Patch.TransitPackage = updatedArray.ToArray();
            var twinDesiredJson = JsonConvert.SerializeObject(twinDesired.ConvertToJObject());
            twin.Properties.Desired = new TwinCollection(twinDesiredJson);

            await _registryManagerWrapper.UpdateTwinAsync(deviceId, twin, twin.ETag);
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while attempting to update ChangeSpecDiagnostics: {ex.Message}");
        }

    }


}