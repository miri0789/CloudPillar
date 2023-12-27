using System.Security.Cryptography.X509Certificates;

namespace Backend.BEApi.Wrappers.Interfaces;
public interface IX509CertificateWrapper
{
    X509Certificate2 CreateCertificate(byte[] bytes);
}