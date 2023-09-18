
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public interface ISymmetricKeyWrapper
{
    // X509Store GetStore(StoreLocation storeLocation);
    
    SecurityProviderSymmetricKey GetSecurityProvider(string registrationId, string primaryKey, string? secondKey);

    // DeviceAuthenticationWithX509Certificate GetDeviceAuthentication(string deviceId, X509Certificate2 certificate);
}