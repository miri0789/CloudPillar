

namespace CloudPillar.Agent.Handlers;
public interface ISymmetricKeyDeviceClientHandler
{
    Task ProvisionWithSymmetricKeyAsync(string registrationId, string primaryKey, string scopeId, string globalDeviceEndpoint, CancellationToken cancellationToken);
    Task<bool> AuthorizationAsync(CancellationToken cancellationToken);
}