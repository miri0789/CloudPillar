using System.Security.Cryptography.X509Certificates;

namespace CloudPillar.Agent.Handlers;
public interface IServerIdentityHandler
{
    Task HandleKnownIdentitiesFromCertificatesAsync(CancellationToken cancellationToken);
    Task<string> GetPublicKeyFromCertificate(X509Certificate2 certificate);

}