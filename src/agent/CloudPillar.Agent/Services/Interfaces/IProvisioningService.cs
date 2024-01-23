namespace CloudPillar.Agent.Sevices.Interfaces;

public interface IProvisioningService
{
    Task ProvisinigSymetricKeyAsync(CancellationToken cancellationToken);
}
