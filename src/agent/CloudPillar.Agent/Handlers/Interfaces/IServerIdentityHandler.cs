using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;
public interface IServerIdentityHandler
{

    Task HandleKnownIdentitiesFromCertificatesAsync(CancellationToken cancellationToken);
}