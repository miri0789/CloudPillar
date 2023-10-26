namespace CloudPillar.Agent.Handlers;
public interface ISymmetricKeyProvisioningHandler 
{
    Task ProvisioningAsync(string deviceId, CancellationToken cancellationToken);
    Task<bool> AuthorizationAsync(CancellationToken cancellationToken);
}