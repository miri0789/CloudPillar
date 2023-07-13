using System.Text;
using System.Text.Json;
using Backend.Iotlistener.Interfaces;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using shared.Entities.Events;

namespace Backend.Iotlistener.Services;

class AzureStreamProcessorFactory : IEventProcessorFactory
{
    private readonly IFirmwareUpdateService _firmwareUpdateService;
    private readonly ISigningService _signingService;
    private readonly string _partitionId;
    private readonly IEnvironmentsWrapper _environmentsWrapper;

    public AzureStreamProcessorFactory(IFirmwareUpdateService firmwareUpdateService,
     ISigningService signingService, IEnvironmentsWrapper environmentsWrapper, string partitionId)
    {
        ArgumentNullException.ThrowIfNull(firmwareUpdateService);
        ArgumentNullException.ThrowIfNull(signingService);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);

        _environmentsWrapper = environmentsWrapper;
        _firmwareUpdateService = firmwareUpdateService;
        _signingService = signingService;
        _partitionId = partitionId;
    }

    public IEventProcessor CreateEventProcessor(PartitionContext context)
    {
        if (string.IsNullOrEmpty(_partitionId) || context.PartitionId == _partitionId)
        {
            return new AgentEventProcessor(_firmwareUpdateService, _signingService, _environmentsWrapper);
        }

        return new NullEventProcessor();
    }
}
public class AgentEventProcessor : IEventProcessor
{
    private readonly IFirmwareUpdateService _firmwareUpdateService;
    private readonly ISigningService _signingService;
    private readonly IEnvironmentsWrapper _environmentsWrapper;

    public AgentEventProcessor(IFirmwareUpdateService firmwareUpdateService,
     ISigningService signingService, IEnvironmentsWrapper environmentsWrapper)
    {
        ArgumentNullException.ThrowIfNull(firmwareUpdateService);
        ArgumentNullException.ThrowIfNull(signingService);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);

        _environmentsWrapper = environmentsWrapper;
        _firmwareUpdateService = firmwareUpdateService;
        _signingService = signingService;
    }

    public Task OpenAsync(PartitionContext context)
    {
        Console.WriteLine($"AgentEventProcessor initialized. Partition: '{context.PartitionId}'");
        return Task.CompletedTask;
    }

    public Task CloseAsync(PartitionContext context, CloseReason reason)
    {
        Console.WriteLine($"AgentEventProcessor closing. Partition: '{context.PartitionId}', Reason: '{reason}'");
        return Task.CompletedTask;
    }

    public Task ProcessErrorAsync(PartitionContext context, Exception error)
    {
        Console.WriteLine($"AgentEventProcessor error on Partition: {context.PartitionId}, Error: {error}");
        return Task.CompletedTask;
    }

    // Process events received from the Event Hubs
    public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
    {
        Console.WriteLine($"AgentEventProcessor ProcessEventsAsync. Partition: '{context.PartitionId}'.");

        string drainD2cQueues = _environmentsWrapper.drainD2cQueues;

        foreach (var eventData in messages)
        {
            if (DateTime.UtcNow - eventData.SystemProperties.EnqueuedTimeUtc > TimeSpan.FromMinutes(_environmentsWrapper.messageTimeoutMinutes))
            {
                Console.WriteLine($"Ignoring message older than {_environmentsWrapper.messageTimeoutMinutes} minutes.");
                continue;
            }
            await HandleMessageData(eventData, !String.IsNullOrWhiteSpace(drainD2cQueues), context.PartitionId);
        }
        await context.CheckpointAsync();
    }

    private async Task HandleMessageData(EventData eventData, bool isDrainMode, string partitionId)
    {
        try
        {
            var data = Encoding.UTF8.GetString(eventData!.Body!.Array!, eventData.Body.Offset, eventData.Body.Count);
            AgentEvent agentEvent = JsonSerializer.Deserialize<AgentEvent>(data)!;

            if (agentEvent == null || isDrainMode)
            {
                Console.WriteLine($"Draining on Partition: {partitionId}, Event: {data}");
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
            Console.WriteLine($"Failed parsing message on Partition: {partitionId}, Error: {ex.Message} - Ignoring");
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