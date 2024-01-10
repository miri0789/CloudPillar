using System.Security.Cryptography.X509Certificates;

namespace CloudPillar.Agent.Handlers;
public interface IServerIdentityHandler
{
    Task UpdateKnownIdentitiesFromCertificatesAsync(CancellationToken cancellationToken);
    Task<string> GetPublicKeyFromCertificate(X509Certificate2 x509Certificate2);
    Task<bool> RemoveNonDefaultCertificates(string path);
}