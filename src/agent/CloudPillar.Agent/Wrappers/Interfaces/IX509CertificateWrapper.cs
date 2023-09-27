
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public interface IX509CertificateWrapper
{
    SecurityProviderX509Certificate CreateSecurityProvider(X509Certificate2 certificate);

    DeviceAuthenticationWithX509Certificate CreateDeviceAuthentication(string deviceId, X509Certificate2 certificate);
}