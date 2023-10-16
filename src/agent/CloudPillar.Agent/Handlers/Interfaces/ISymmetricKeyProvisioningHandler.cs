namespace CloudPillar.Agent.Handlers;
public interface ISymmetricKeyProvisioningHandler 
{
    Task ProvisioningAsync(string scopeId, string globalDeviceEndpoint, CancellationToken cancellationToken);
    Task<bool> AuthorizationAsync(CancellationToken cancellationToken);
}