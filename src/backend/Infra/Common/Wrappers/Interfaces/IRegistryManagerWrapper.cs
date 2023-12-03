using Microsoft.Azure.Devices.Shared;

namespace Backend.Infra.Common.Wrappers.Interfaces;
public interface IRegistryManagerWrapper
{
    Task<Twin> GetTwinAsync(string deviceId);
    Task<Twin> UpdateTwinAsync(string deviceId, Twin twinPatch, string etag);
}