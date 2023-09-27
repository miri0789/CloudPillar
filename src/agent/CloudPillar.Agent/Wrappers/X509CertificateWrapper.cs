using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public class X509CertificateWrapper : IX509CertificateWrapper
{
    public DeviceAuthenticationWithX509Certificate CreateDeviceAuthentication(string deviceId, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
        ArgumentNullException.ThrowIfNull(certificate);
        return new DeviceAuthenticationWithX509Certificate(deviceId, certificate);
    }

    public SecurityProviderX509Certificate CreateSecurityProvider(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return new SecurityProviderX509Certificate(certificate);
    }
}
