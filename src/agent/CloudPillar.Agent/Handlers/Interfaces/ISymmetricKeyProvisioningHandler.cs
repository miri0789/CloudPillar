using CloudPillar.Agent.Entities;

namespace CloudPillar.Agent.Handlers;
public interface ISymmetricKeyProvisioningHandler
{
    Task<bool> ProvisioningAsync(string deviceId, CancellationToken cancellationToken);
    Task<DeviceConnectResultEnum> AuthorizationDeviceAsync(CancellationToken cancellationToken);

    Task<bool> IsNewDeviceAsync(CancellationToken cancellationToken);
}