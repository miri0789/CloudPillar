using System.Text;
using System.Text.Json;
using Backend.Iotlistener.Interfaces;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Shared.Entities.Messages;
using Shared.Logger;

namespace Backend.Iotlistener.Processors;

class AzureStreamProcessorFactory : IEventProcessorFactory
{
    private readonly IFirmwareUpdateService _firmwareUpdateService;
    private readonly ISigningService _signingService;
    private readonly IStreamingUploadChunkService _streamingUploadChunkService;
    private readonly IProvisionDeviceCertificateService _provisionDeviceCertificateService;
    private readonly string _partitionId;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;

    public AzureStreamProcessorFactory(IFirmwareUpdateService firmwareUpdateService,
    ISigningService signingService,
    IStreamingUploadChunkService streamingUploadChunkService,
    IProvisionDeviceCertificateService provisionDeviceCertificateService,
    IEnvironmentsWrapper environmentsWrapper,
    ILoggerHandler logger,
    string partitionId)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _firmwareUpdateService = firmwareUpdateService ?? throw new ArgumentNullException(nameof(firmwareUpdateService));
        _streamingUploadChunkService = streamingUploadChunkService ?? throw new ArgumentNullException(nameof(streamingUploadChunkService));
        _provisionDeviceCertificateService = provisionDeviceCertificateService ?? throw new ArgumentNullException(nameof(provisionDeviceCertificateService));
        _signingService = signingService ?? throw new ArgumentNullException(nameof(signingService));
        _partitionId = partitionId;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IEventProcessor CreateEventProcessor(PartitionContext context)
    {
        if (string.IsNullOrEmpty(_partitionId) || context.PartitionId == _partitionId)
        {
            return new AgentEventProcessor(_firmwareUpdateService, _signingService, _streamingUploadChunkService, _provisionDeviceCertificateService, _environmentsWrapper, _logger);
        }

        return new NullEventProcessor();
    }
}
public class AgentEventProcessor : IEventProcessor
{
    private readonly IFirmwareUpdateService _firmwareUpdateService;
    private readonly ISigningService _signingService;
    private readonly IStreamingUploadChunkService _streamingUploadChunkService;
    private readonly IProvisionDeviceCertificateService _provisionDeviceCertificateService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;

    public AgentEventProcessor(IFirmwareUpdateService firmwareUpdateService,
    ISigningService signingService,
    IStreamingUploadChunkService streamingUploadChunkService,
    IProvisionDeviceCertificateService provisionDeviceCertificateService,
    IEnvironmentsWrapper environmentsWrapper,
    ILoggerHandler logger)
    {
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _firmwareUpdateService = firmwareUpdateService ?? throw new ArgumentNullException(nameof(firmwareUpdateService));
        _streamingUploadChunkService = streamingUploadChunkService ?? throw new ArgumentNullException(nameof(streamingUploadChunkService));
        _provisionDeviceCertificateService = provisionDeviceCertificateService ?? throw new ArgumentNullException(nameof(provisionDeviceCertificateService));
        _signingService = signingService ?? throw new ArgumentNullException(nameof(signingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task OpenAsync(PartitionContext context)
    {
        _logger.Info($"AgentEventProcessor initialized. Partition: '{context.PartitionId}'");
        return Task.CompletedTask;
    }

    public Task CloseAsync(PartitionContext context, CloseReason reason)
    {
        _logger.Info($"AgentEventProcessor closing. Partition: '{context.PartitionId}', Reason: '{reason}'");
        return Task.CompletedTask;
    }

    public Task ProcessErrorAsync(PartitionContext context, Exception error)
    {
        _logger.Info($"AgentEventProcessor error on Partition: {context.PartitionId}, Error: {error}");
        return Task.CompletedTask;
    }

    // Process events received from the Event Hubs
    public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
    {
        _logger.Info($"AgentEventProcessor ProcessEventsAsync. Partition: '{context.PartitionId}'.");

        string drainD2cQueues = _environmentsWrapper.drainD2cQueues;

        foreach (var eventData in messages)
        {
            if (DateTime.UtcNow - eventData.SystemProperties.EnqueuedTimeUtc > TimeSpan.FromMinutes(_environmentsWrapper.messageTimeoutMinutes))
            {
                _logger.Warn($"Ignoring message older than {_environmentsWrapper.messageTimeoutMinutes} minutes.");
                continue;
            }
            await HandleMessageAsync(eventData, !String.IsNullOrWhiteSpace(drainD2cQueues), context.PartitionId);
        }
        await context.CheckpointAsync();
    }

    private async Task HandleMessageAsync(EventData eventData, bool isDrainMode, string partitionId)
    {
        try
        {
            var data = Encoding.UTF8.GetString(eventData!.Body!.Array!, eventData.Body.Offset, eventData.Body.Count);
            D2CMessage d2CMessage = JsonSerializer.Deserialize<D2CMessage>(data)!;

            if (d2CMessage == null || isDrainMode)
            {
                _logger.Warn($"Draining on Partition: {partitionId}, Event: {data}");
                return;
            }

            string iothubConnectionDeviceId = _environmentsWrapper.iothubConnectionDeviceId;
            string? deviceId = eventData.SystemProperties[iothubConnectionDeviceId]?.ToString();

            if (!String.IsNullOrWhiteSpace(deviceId) && d2CMessage.MessageType != null)
            {
                switch (d2CMessage.MessageType)
                {
                    case D2CMessageType.FirmwareUpdateReady:
                        var firmwareUpdateEvent = JsonSerializer.Deserialize<FirmwareUpdateEvent>(data)!;
                        await _firmwareUpdateService.SendFirmwareUpdateAsync(deviceId, firmwareUpdateEvent);
                        break;
                    case D2CMessageType.SignTwinKey:
                        var signTwinKeyEvent = JsonSerializer.Deserialize<SignEvent>(data)!;
                        await _signingService.CreateTwinKeySignature(deviceId, signTwinKeyEvent);
                        break;
                    case D2CMessageType.StreamingUploadChunk:
                        var streamingUploadChunkEvent = JsonSerializer.Deserialize<StreamingUploadChunkEvent>(data)!;
                        await _streamingUploadChunkService.UploadStreamToBlob(streamingUploadChunkEvent, deviceId);
                        break;
                    case D2CMessageType.ProvisionDeviceCertificate:
                        var provisionDeviceCertificateEvent = JsonSerializer.Deserialize<ProvisionDeviceCertificateEvent>(data)!;
                        await _provisionDeviceCertificateService.ProvisionDeviceCertificateAsync(deviceId, provisionDeviceCertificateEvent);
                        break;
                    case D2CMessageType.DeleteBlob:
                        var deleteBlobEvent = JsonSerializer.Deserialize<DeleteBlobEvent>(data)!;
                        await _provisionDeviceCertificateService.ProvisionDeviceCertificateAsync(deviceId, new ProvisionDeviceCertificateEvent());

                        await _streamingUploadChunkService.DeleteBlob(deleteBlobEvent);
                        break;
                }

            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed parsing message on Partition: {partitionId}, Error: {ex.Message} - Ignoring");
        }
    }

}


class NullEventProcessor : IEventProcessor
{
    public Task CloseAsync(PartitionContext context, CloseReason reason)
    {
        return Task.CompletedTask;
    }

    public Task OpenAsync(PartitionContext context)
    {
        return Task.CompletedTask;
    }

    public Task ProcessErrorAsync(PartitionContext context, Exception error)
    {
        return Task.CompletedTask;
    }

    public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
    {
        return Task.CompletedTask;
    }
}