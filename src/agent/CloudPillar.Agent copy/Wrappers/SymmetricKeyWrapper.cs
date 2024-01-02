using System.Security.Cryptography;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public class SymmetricKeyWrapper : ISymmetricKeyWrapper
{
    public SecurityProviderSymmetricKey GetSecurityProvider(string registrationId, string primaryKey, string? secondKey)
    {
        ArgumentNullException.ThrowIfNull(registrationId);
        ArgumentNullException.ThrowIfNull(primaryKey);
        return new SecurityProviderSymmetricKey(registrationId, primaryKey, secondKey);
    }

    public DeviceAuthenticationWithRegistrySymmetricKey GetDeviceAuthentication(string deviceId, string deviceKey)
    {
        ArgumentNullException.ThrowIfNull(deviceId);
        ArgumentNullException.ThrowIfNull(deviceKey);
        return new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey);
    }
    public HMACSHA256 CreateHMAC(string primaryKey)
    {
        return new HMACSHA256(Convert.FromBase64String(primaryKey));
    }
}
