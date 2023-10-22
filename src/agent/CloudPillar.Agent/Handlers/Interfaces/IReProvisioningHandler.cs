using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public interface IReprovisioningHandler
{
    Task<bool> HandleReprovisioningMessageAsync(ReprovisioningMessage message, CancellationToken cancellationToken);

    Task HandleRequestDeviceCertificateAsync(RequestDeviceCertificateMessage message, CancellationToken cancellationToken);
}