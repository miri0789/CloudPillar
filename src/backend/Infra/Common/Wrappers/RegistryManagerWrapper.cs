using Backend.Infra.Common.Wrappers.Interfaces;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace Backend.Infra.Common.Wrappers;

public class RegistryManagerWrapper : IRegistryManagerWrapper
{
    private readonly RegistryManager _registryManager;
    private readonly ICommonEnvironmentsWrapper _environmentsWrapper;

    public RegistryManagerWrapper(ICommonEnvironmentsWrapper environmentsWrapper)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _registryManager = RegistryManager.CreateFromConnectionString(_environmentsWrapper.iothubConnectionString);
    }

    public async Task<Twin> GetTwinAsync(string deviceId)
    {
        return await _registryManager.GetTwinAsync(deviceId);
    }

    public async Task<Twin> UpdateTwinAsync(string deviceId, Twin twinPatch, string etag)
    {
        return await _registryManager.UpdateTwinAsync(deviceId, twinPatch, etag);
    }
}