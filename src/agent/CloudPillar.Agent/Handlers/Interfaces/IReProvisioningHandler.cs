using Microsoft.Azure.Devices.Client;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public interface IReprovisioningHandler
{
    Task HandleReprovisioningMessageAsync(Message recivedMessage, ReprovisioningMessage message, CancellationToken cancellationToken);

    Task HandleRequestDeviceCertificateAsync(RequestDeviceCertificateMessage message, CancellationToken cancellationToken);
}