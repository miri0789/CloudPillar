using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public class X509CertificateWrapper : IX509CertificateWrapper
{

    public X509Store GetStore(StoreLocation storeLocation)
    {
        ArgumentNullException.ThrowIfNull(storeLocation);
        return new X509Store(storeLocation);
    }
    
    public DeviceAuthenticationWithX509Certificate GetDeviceAuthentication(string deviceId, X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(deviceId);
        ArgumentNullException.ThrowIfNull(certificate);
        return new DeviceAuthenticationWithX509Certificate(deviceId, certificate);
    }

    public SecurityProviderX509Certificate GetSecurityProvider(X509Certificate2 certificate)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        return new SecurityProviderX509Certificate(certificate);
    }
}
