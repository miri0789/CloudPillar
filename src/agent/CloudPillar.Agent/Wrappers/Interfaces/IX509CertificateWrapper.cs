
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public interface IX509CertificateWrapper
{
    X509Store GetStore(StoreLocation storeLocation);
    
    SecurityProviderX509Certificate GetSecurityProvider(X509Certificate2 certificate);

    DeviceAuthenticationWithX509Certificate GetDeviceAuthentication(string deviceId, X509Certificate2 certificate);
}