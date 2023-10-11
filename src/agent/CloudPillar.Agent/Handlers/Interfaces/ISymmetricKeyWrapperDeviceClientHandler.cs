

namespace CloudPillar.Agent.Handlers;
public interface ISymmetricKeyWrapperDeviceClientHandler
{
    Task ProvisioningAsync(string registrationId, string primaryKey, string scopeId, string globalDeviceEndpoint, CancellationToken cancellationToken);
    Task<bool> AuthorizationAsync(CancellationToken cancellationToken);
}