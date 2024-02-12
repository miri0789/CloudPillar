namespace CloudPillar.Agent.Handlers;
public interface ISymmetricKeyProvisioningHandler
{
    Task<bool> ProvisioningAsync(string deviceId, CancellationToken cancellationToken);
    Task<bool> AuthorizationDeviceAsync(CancellationToken cancellationToken);

    Task<bool> IsNewDeviceAsync(CancellationToken cancellationToken);
}