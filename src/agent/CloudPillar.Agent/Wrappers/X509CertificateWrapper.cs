using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Shared.Entities.Authentication;

namespace CloudPillar.Agent.Wrappers;
public class X509CertificateWrapper : IX509CertificateWrapper
{
        public X509Store GetStore(StoreLocation storeLocation)
    {
        ArgumentNullException.ThrowIfNull(storeLocation);
        return new X509Store(storeLocation);
    }

    public X509Certificate2Collection GetCertificates(OpenFlags openFlags)
    {
        using (X509Store store = new X509Store(StoreLocation.CurrentUser))
        {
            store.Open(openFlags);
            return store.Certificates;
        }
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
