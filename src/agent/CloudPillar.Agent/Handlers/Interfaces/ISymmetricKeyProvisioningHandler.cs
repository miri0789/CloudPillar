namespace CloudPillar.Agent.Handlers;
public interface ISymmetricKeyProvisioningHandler 
{
    Task ProvisioningAsync(string registrationId, string primaryKey, string scopeId, string globalDeviceEndpoint, CancellationToken cancellationToken);
    Task<bool> AuthorizationAsync(CancellationToken cancellationToken);
}