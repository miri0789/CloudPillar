using System.Security.Cryptography.X509Certificates;

namespace CloudPillar.Agent.Sevices.interfaces;

public interface IRemoveX509Certificates
{
    void RemoveX509CertificatesFromStore();
    void RemoveCertificatesFromStore(X509Store store, string thumbprint);
}