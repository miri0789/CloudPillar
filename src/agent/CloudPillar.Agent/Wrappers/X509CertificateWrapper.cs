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
    private readonly X509Store _store;

    public X509CertificateWrapper()
    {
        _store = new X509Store(StoreLocation.CurrentUser);
    }

    public void Open(OpenFlags flags)
    {
        _store.Open(flags);
    }

    public X509Certificate2Collection Certificates => _store.Certificates;

    public X509Certificate2Collection Find(X509FindType findType, string findValue, bool validOnly)
    {
        return _store.Certificates.Find(findType, findValue, validOnly);
    }

    public void RemoveRange(X509Certificate2Collection collection)
    {
        _store.RemoveRange(collection);
    }

    public void Close()
    {
        _store.Close();
    }

    public X509Certificate2Collection CreateCertificateCollecation(X509Certificate2[] certificates)
    {
        return new X509Certificate2Collection(certificates);
    }

    public void Add(X509Certificate2 x509Certificate)
    {
        _store.Add(x509Certificate);
    }
    
    public X509Certificate2 CreateFromBytes(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
    {
        return new X509Certificate2(rawData, password, keyStorageFlags);
    }
    
    public DeviceAuthenticationWithX509Certificate GetDeviceAuthentication(string deviceId, X509Certificate2 certificate)
    {
        return new DeviceAuthenticationWithX509Certificate(deviceId, certificate);
    }

    public SecurityProviderX509Certificate GetSecurityProvider(X509Certificate2 certificate)
    {
        return new SecurityProviderX509Certificate(certificate);
    }
}
