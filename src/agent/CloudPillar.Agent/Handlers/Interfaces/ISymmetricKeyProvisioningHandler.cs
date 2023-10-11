namespace CloudPillar.Agent.Handlers;
public interface ISymmetricKeyProvisioningHandler 
{
    Task ProvisionWithSymmetricKeyAsync(string registrationId, string primaryKey, string scopeId, string globalDeviceEndpoint, CancellationToken cancellationToken);
    Task<bool> AuthorizationAsync(CancellationToken cancellationToken);
}