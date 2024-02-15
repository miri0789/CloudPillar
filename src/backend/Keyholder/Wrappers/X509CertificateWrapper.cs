using System.Security.Cryptography.X509Certificates;
using Backend.Keyholder.Wrappers.Interfaces;

namespace Backend.Keyholder.Wrappers;
public class X509CertificateWrapper : IX509CertificateWrapper
{
   
    public X509Certificate2 CreateCertificateFrombytes(byte[] bytes)
    {
        return new X509Certificate2(bytes);
    }
}
