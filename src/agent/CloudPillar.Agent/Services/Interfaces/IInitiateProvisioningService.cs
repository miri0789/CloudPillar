namespace CloudPillar.Agent.Sevices.interfaces;

public interface IInitiateProvisioningService
{
    Task<string> InitiateProvisioningAsync(string deviceId, string secretKey, CancellationToken cancellationToken);
}