using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Shared;

namespace Backend.Infra.Common.Wrappers.Interfaces;
public interface IRegistryManagerWrapper
{
    RegistryManager CreateFromConnectionString();
    Task<Twin> GetTwinAsync(RegistryManager registryManager, string deviceId);
    Task<Twin> UpdateTwinAsync(RegistryManager registryManager, string deviceId, Twin twinPatch, string etag);
    Task<IEnumerable<Device>> GetIotDevicesAsync(RegistryManager registryManager);
}