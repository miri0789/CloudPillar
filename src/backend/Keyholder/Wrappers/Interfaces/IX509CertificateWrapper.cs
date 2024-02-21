
using System.Security.Cryptography.X509Certificates;

namespace Backend.Keyholder.Wrappers.Interfaces;
public interface IX509CertificateWrapper
{
    X509Certificate2 CreateCertificateFrombytes(byte[] bytes);
}