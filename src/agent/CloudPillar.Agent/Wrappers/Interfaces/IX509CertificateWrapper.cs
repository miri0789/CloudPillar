
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public interface IX509CertificateWrapper
{
    X509Store Open(OpenFlags flags, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.LocalMachine);

    X509Certificate2Collection GetCertificates(X509Store store);

    X509Certificate2Collection Find(X509Store store, X509FindType findType, string findValue, bool validOnly);

    void RemoveRange(X509Store store, X509Certificate2Collection collection);

    void Add(X509Store store, X509Certificate2 x509Certificate);

    X509Certificate2Collection CreateCertificateCollecation(X509Certificate2[] certificates);

    X509Certificate2 CreateFromBytes(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags);

    X509Certificate2 CreateFromFile(string certificatePath);

    SecurityProviderX509Certificate GetSecurityProvider(X509Certificate2 certificate);

    DeviceAuthenticationWithX509Certificate GetDeviceAuthentication(string deviceId, X509Certificate2 certificate);
}