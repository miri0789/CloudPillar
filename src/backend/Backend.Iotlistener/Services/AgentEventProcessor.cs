using System.Text;
using System.Text.Json;
using Backend.Iotlistener.Interfaces;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Shared.Entities.Events;
using Shared.Logger;

namespace Backend.Iotlistener.Services;

class AzureStreamProcessorFactory : IEventProcessorFactory
{
    private readonly IFirmwareUpdateService _firmwareUpdateService;
    private readonly ISigningService _signingService;
    private readonly string _partitionId;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;

    public AzureStreamProcessorFactory(IFirmwareUpdateService firmwareUpdateService,
     ISigningService signingService, IEnvironmentsWrapper environmentsWrapper, ILoggerHandler logger, string partitionId)
    {
        ArgumentNullException.ThrowIfNull(firmwareUpdateService);
        ArgumentNullException.ThrowIfNull(signingService);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        ArgumentNullException.ThrowIfNull(logger);

        _environmentsWrapper = environmentsWrapper;
        _firmwareUpdateService = firmwareUpdateService;
        _signingService = signingService;
        _partitionId = partitionId;
        _logger = logger;
    }

    public IEventProcessor CreateEventProcessor(PartitionContext context)
    {
        if (string.IsNullOrEmpty(_partitionId) || context.PartitionId == _partitionId)
        {
            return new AgentEventProcessor(_firmwareUpdateService, _signingService, _environmentsWrapper, _logger);
        }

        return new NullEventProcessor();
    }
}
public class AgentEventProcessor : IEventProcessor
{
    private readonly IFirmwareUpdateService _firmwareUpdateService;
    private readonly ISigningService _signingService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ILoggerHandler _logger;

    public AgentEventProcessor(IFirmwareUpdateService firmwareUpdateService,
     ISigningService signingService, IEnvironmentsWrapper environmentsWrapper, ILoggerHandler logger)
    {
        ArgumentNullException.ThrowIfNull(firmwareUpdateService);
        ArgumentNullException.ThrowIfNull(signingService);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        ArgumentNullException.ThrowIfNull(logger);

        _environmentsWrapper = environmentsWrapper;
        _firmwareUpdateService = firmwareUpdateService;
        _signingService = signingService;
        _logger = logger;
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
            AgentEvent agentEvent = JsonSerializer.Deserialize<AgentEvent>(data)!;

            if (agentEvent == null || isDrainMode)
            {
                _logger.Warn($"Draining on Partition: {partitionId}, Event: {data}");
                return;
            }

            string iothubConnectionDeviceId = _environmentsWrapper.iothubConnectionDeviceId;
            string? deviceId = eventData.SystemProperties[iothubConnectionDeviceId]?.ToString();

            if (!String.IsNullOrWhiteSpace(deviceId) && agentEvent.EventType != null)
            {
                switch (agentEvent.EventType)
                {
                    case EventType.FirmwareUpdateReady:
                        {
                            var firmwareUpdateEvent = JsonSerializer.Deserialize<FirmwareUpdateEvent>(data)!;
                            await _firmwareUpdateService.SendFirmwareUpdateAsync(deviceId, firmwareUpdateEvent);
                            break;
                        }
                    case EventType.SignTwinKey:
                        {
                            var signTwinKeyEvent = JsonSerializer.Deserialize<SignEvent>(data)!;
                            await _signingService.CreateTwinKeySignature(deviceId, signTwinKeyEvent);
                            break;
                        }
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