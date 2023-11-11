using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;
using Polly;
using Shared.Entities.Messages;
using Shared.Entities.Factories;
using Backend.BlobStreamer.Interfaces;
using Shared.Logger;
using Backend.Infra.Common;
using Microsoft.Azure.Devices.Shared;
using Shared.Entities.Twin;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

    public async Task AddDesiredToTwin(string deviceId, DownloadAction downloadAction)
    {
        var twin = await _registryManagerWrapper.GetTwinAsync(deviceId);

        var twinJson = JObject.FromObject(twin.Properties.Desired);

        string desiredJson = twin.Properties.Desired.ToJson();
        var twinDesired = JsonConvert.DeserializeObject<TwinDesired>(desiredJson,
        new JsonSerializerSettings
        {
            Converters = new List<JsonConverter> {
                            new TwinDesiredConverter(), new TwinActionConverter() }
        });


        if (twinDesired.ChangeSpecDiagnostics?.Patch?.TransitPackage == null)
        {
            twinDesired.ChangeSpecDiagnostics.Patch.TransitPackage = new TwinAction[] { };
        }
        twinDesired.ChangeSpecDiagnostics.Patch.TransitPackage.ToList().Add(downloadAction);

        // Convert the modified twinDesired back to JSON and update the twin properties.
        twin.Properties.Desired = new TwinCollection(twinDesired.ToString());

        // twin.Properties.Desired[""] = JsonConvert.SeriaslizeObject(twinReported).
        await _registryManagerWrapper.UpdateTwinAsync(deviceId, twin, twin.ETag);


    }
}