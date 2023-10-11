using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public interface IReProvisioningHandler
{
    Task HandleReProvisioningMessageAsync(ReProvisioningMessage message, CancellationToken cancellationToken);

    Task HandleRequestDeviceCertificateAsync(RequestDeviceCertificateMessage message, CancellationToken cancellationToken);
}