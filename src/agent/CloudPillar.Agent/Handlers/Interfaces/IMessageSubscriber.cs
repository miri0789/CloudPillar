﻿using CloudPillar.Agent.Entities;
using Microsoft.Azure.Devices.Client;
using Shared.Entities.Messages;

namespace CloudPillar.Agent.Handlers;
public interface IMessageSubscriber
{
    Task<ActionToReport> HandleDownloadMessageAsync(DownloadBlobChunkMessage message, CancellationToken cancellationToken);

    Task HandleReprovisioningMessageAsync(Message recivedMessage,ReprovisioningMessage message, CancellationToken cancellationToken);

    Task HandleRequestDeviceCertificateAsync(RequestDeviceCertificateMessage message, CancellationToken cancellationToken);
}
