using System.Security.Cryptography.X509Certificates;

namespace CloudPillar.Agent.Handlers;
public interface IServerIdentityHandler
{
    Task UpdateKnownIdentitiesFromCertificatesAsync(CancellationToken cancellationToken);
    Task<string> GetPublicKeyFromCertificateFileAsync(string certificatePath);
    bool CheckCertificateNotExpired(string path);
    Task RemoveNonDefaultCertificatesAsync(string path);
}