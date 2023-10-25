
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public interface IX509CertificateWrapper
{
    void Open(OpenFlags flags);

    X509Certificate2Collection Certificates { get; }

    X509Certificate2Collection Find(X509FindType findType, string findValue, bool validOnly);

    void RemoveRange(X509Certificate2Collection collection);

    void Add(X509Certificate2 x509Certificate);

    void Close();

    X509Certificate2Collection CreateCertificateCollecation(X509Certificate2[] certificates);

    X509Certificate2 CreateFromBytes(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags);
    
    SecurityProviderX509Certificate GetSecurityProvider(X509Certificate2 certificate);

    DeviceAuthenticationWithX509Certificate GetDeviceAuthentication(string deviceId, X509Certificate2 certificate);
}