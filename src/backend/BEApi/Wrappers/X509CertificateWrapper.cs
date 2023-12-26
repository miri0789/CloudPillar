using System.Security.Cryptography.X509Certificates;
using Backend.BEApi.Wrappers.Interfaces;

namespace Backend.BEApi.Wrappers;
public class X509CertificateWrapper : IX509CertificateWrapper
{
    public X509Certificate2 CreateCertificate(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return new X509Certificate2(bytes);
    }
}