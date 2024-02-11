using Microsoft.Azure.Devices.Shared;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;
using Newtonsoft.Json;
using Shared.Entities.Twin;
using Shared.Entities.Utilities;
using Shared.Logger;
using Microsoft.Azure.Devices;
using System.Text;

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

    public async Task AddDesiredRecipeAsync(string deviceId, string changeSpecKey, DownloadAction downloadAction, int order = SharedConstants.DEFAULT_CHANGE_SPEC_ORDER_VALUE, string transactionsKey = SharedConstants.DEFAULT_TRANSACTIONS_KEY)
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
                if (order != twinDesiredChangeSpec?.Order)
                {
                    twinDesiredChangeSpec.Order = order;
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

    public async Task<TwinDesired> AddChangeSpec(RegistryManager registryManager, string deviceId, string changeSpecKey, TwinChangeSpec assignChangeSpec, string signature)
    {
        ArgumentNullException.ThrowIfNull(deviceId);

        try
        {
            var twin = await _registryManagerWrapper.GetTwinAsync(registryManager, deviceId);
            TwinDesired twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();
            twinDesired.ChangeSpec ??= new Dictionary<string, TwinChangeSpec>();
            twinDesired.ChangeSign ??= new Dictionary<string, string>();
            var twinDesiredChangeSpec = twinDesired.GetDesiredChangeSpecByKey(changeSpecKey);
            if (twinDesiredChangeSpec is not null)
            {
                twinDesired.ChangeSpec[changeSpecKey] = null;
                twinDesired.ChangeSign[changeSpecKey.GetSignKeyByChangeSpec()] = null;
                await UpdateTwinAsync(twinDesired, registryManager, deviceId, twin);
                twin = await _registryManagerWrapper.GetTwinAsync(registryManager, deviceId);
                twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();
                twinDesired.ChangeSpec ??= new Dictionary<string, TwinChangeSpec>();
                twinDesired.ChangeSign ??= new Dictionary<string, string>();
            }
            twinDesired.ChangeSpec.Add(changeSpecKey, assignChangeSpec);
            twinDesired.ChangeSign.Add(changeSpecKey.GetSignKeyByChangeSpec(), signature);
            await UpdateTwinAsync(twinDesired, registryManager, deviceId, twin);
            return twinDesired;
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while attempting to add new changeSpec {changeSpecKey}: {ex.Message}");
            return null;
        }
    }

    public Byte[] GetChangeSpecDataToSign(TwinChangeSpec changeSpec)
    {
        var dataToSign = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(changeSpec));
        return dataToSign;
    }

    public async Task<TwinDesired> GetTwinDesiredAsync(string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);

        try
        {
            using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
            {
                var twin = await _registryManagerWrapper.GetTwinAsync(registryManager, deviceId);
                return twin.Properties.Desired.ToJson().ConvertToTwinDesired();
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"An error occurred while attempting to get twin desired: {ex.Message}");
            return null;
        }
    }

    public async Task SignTwinDesiredAsync(TwinDesired twinDesired, string deviceId, string changeSignKey, string signData)
    {
        using (var registryManager = _registryManagerWrapper.CreateFromConnectionString())
        {
            var twin = await _registryManagerWrapper.GetTwinAsync(registryManager, deviceId);
            twinDesired.SetDesiredChangeSignByKey(changeSignKey, signData);

            twin.Properties.Desired = new TwinCollection(twinDesired.ConvertToJObject().ToString());
            await UpdateTwinAsync(twinDesired, registryManager, deviceId, twin);
        }
    }

    private async Task UpdateTwinAsync(TwinDesired twinDesired, RegistryManager registryManager, string deviceId, Twin twin)
    {
        var twinDesiredJson = JsonConvert.SerializeObject(twinDesired.ConvertToJObject());
        twin.Properties.Desired = new TwinCollection(twinDesiredJson);
        await _registryManagerWrapper.UpdateTwinAsync(registryManager, deviceId, twin, twin.ETag);
    }
}