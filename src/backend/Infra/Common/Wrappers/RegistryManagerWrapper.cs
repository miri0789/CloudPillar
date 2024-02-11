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
        var twin = await registryManager.GetTwinAsync(deviceId);
        if (twin == null)
        {
            throw new KeyNotFoundException($"Device {deviceId} not found");
        }
        return twin;
    }

    public async Task<Twin> UpdateTwinAsync(RegistryManager registryManager, string deviceId, Twin twinPatch, string etag)
    {
        return await registryManager.UpdateTwinAsync(deviceId, twinPatch, etag);
    }

    public async Task<IEnumerable<Device>> GetIotDevicesAsync(RegistryManager registryManager, int maxCountDevices)
    {
        return await registryManager.GetDevicesAsync(maxCountDevices);
    }

    public async Task RemoveDeviceAsync(RegistryManager registryManager, string deviceId)
    {
        await registryManager.RemoveDeviceAsync(deviceId);
    }
}