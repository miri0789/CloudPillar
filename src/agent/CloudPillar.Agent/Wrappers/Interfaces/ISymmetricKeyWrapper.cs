
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public interface ISymmetricKeyWrapper
{
    SecurityProviderSymmetricKey GetSecurityProvider(string registrationId, string primaryKey, string? secondKey);
    DeviceAuthenticationWithRegistrySymmetricKey GetDeviceAuthentication(string deviceId, string deviceKey);
}