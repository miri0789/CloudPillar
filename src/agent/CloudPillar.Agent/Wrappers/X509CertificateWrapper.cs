using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace CloudPillar.Agent.Wrappers;
public class X509CertificateWrapper : IX509CertificateWrapper
{
    public X509Store Open(OpenFlags flags, StoreLocation storeLocation, StoreName storeName = StoreName.My)
    {
        var store = new X509Store(storeName, storeLocation);
        store.Open(flags);
        return store;
    }

    public X509Certificate2Collection GetCertificates(X509Store store)
    {
        return store.Certificates;
    }

    public X509Certificate2Collection Find(X509Store store, X509FindType findType, string findValue, bool validOnly)
    {
        return store.Certificates.Find(findType, findValue, validOnly);
    }

    public void RemoveRange(X509Store store, X509Certificate2Collection collection)
    {
        store.RemoveRange(collection);
    }

    public X509Certificate2Collection CreateCertificateCollecation(X509Certificate2[] certificates)
    {
        return new X509Certificate2Collection(certificates);
    }

    public void Add(X509Store store, X509Certificate2 x509Certificate)
    {
        store.Add(x509Certificate);
    }

    public X509Certificate2 CreateFromBytes(byte[] rawData, string password, X509KeyStorageFlags keyStorageFlags)
    {
        return new X509Certificate2(rawData, password, keyStorageFlags);
    }

    public X509Certificate2 CreateFromFile(string certificatePath)
    {
        return new X509Certificate2(certificatePath);
    }


    public DeviceAuthenticationWithX509Certificate GetDeviceAuthentication(string deviceId, X509Certificate2 certificate)
    {
        return new DeviceAuthenticationWithX509Certificate(deviceId, certificate);
    }

    public SecurityProviderX509Certificate GetSecurityProvider(X509Certificate2 certificate)
    {
        return new SecurityProviderX509Certificate(certificate);
    }

    public byte[] ExportSubjectPublicKeyInfo(X509Certificate2 certificate)
    {
        return certificate.PublicKey.ExportSubjectPublicKeyInfo();
    }

    public string GetAlgorithmFriendlyName(X509Certificate2 certificate)
    {
        return certificate.SignatureAlgorithm.FriendlyName;
    }
}
