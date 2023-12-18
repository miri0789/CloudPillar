using Backend.Infra.Common.Wrappers.Interfaces;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace Backend.Infra.Common.Wrappers;

public class RegistryManagerWrapper : IRegistryManagerWrapper
{
    private readonly ICommonEnvironmentsWrapper _environmentsWrapper;

    public RegistryManagerWrapper(ICommonEnvironmentsWrapper environmentsWrapper)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
    }

    public RegistryManager CreateFromConnectionString()
    {
        return RegistryManager.CreateFromConnectionString(_environmentsWrapper.iothubConnectionString);
    }

    public async Task<Twin> GetTwinAsync(RegistryManager registryManager, string deviceId)
    {
        return await registryManager.GetTwinAsync(deviceId);
    }

    public async Task<Twin> UpdateTwinAsync(RegistryManager registryManager,string deviceId, Twin twinPatch, string etag)
    {
        return await registryManager.UpdateTwinAsync(deviceId, twinPatch, etag);
    }
}