using System.Text;
using System.Text.Json;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;

namespace iotlistener;

class AzureStreamProcessorFactory : IEventProcessorFactory
{
    private readonly IFirmwareUpdateService _firmwareUpdateService;
    private readonly ISigningService _signingService;

    public AzureStreamProcessorFactory(IFirmwareUpdateService firmwareUpdateService, ISigningService signingService)
    {
        _firmwareUpdateService = firmwareUpdateService;
        _signingService = signingService;
    }

    IEventProcessor IEventProcessorFactory.CreateEventProcessor(PartitionContext context)
    {
        return new AgentEventProcessor(_firmwareUpdateService, _signingService);
    }
}

public class AgentEventProcessor : IEventProcessor
{
    private readonly IFirmwareUpdateService _firmwareUpdateService;
    private readonly ISigningService _signingService;

    public AgentEventProcessor(IFirmwareUpdateService firmwareUpdateService, ISigningService signingService)
    {
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
        foreach (var eventData in messages)
        {
            try
            {
                int.TryParse(Environment.GetEnvironmentVariable(Constants.messageTimeoutMinutes), out int messageTimeoutMinutes);
                if (DateTime.UtcNow - eventData.SystemProperties.EnqueuedTimeUtc > TimeSpan.FromMinutes(messageTimeoutMinutes))
                {
                    Console.WriteLine("Ignoring message older than 1 hour.");
                    continue;
                }

                var data = Encoding.UTF8.GetString(eventData!.Body!.Array!, eventData.Body.Offset, eventData.Body.Count);
                AgentEvent agentEvent = JsonSerializer.Deserialize<AgentEvent>(data)!;

                string drainD2cQueues = Environment.GetEnvironmentVariable(Constants.drainD2cQueues);
                if (agentEvent == null || !String.IsNullOrEmpty(drainD2cQueues))
                {
                    Console.WriteLine($"Draining on Partition: {context.PartitionId}, Event: {data}");
                    continue;
                }

                string iothubConnectionDeviceId = Environment.GetEnvironmentVariable(Constants.iothubConnectionDeviceId);
                string? deviceId = eventData.SystemProperties[iothubConnectionDeviceId]?.ToString();

                if (!String.IsNullOrEmpty(deviceId) && agentEvent.eventType != null)
                {
                    switch (agentEvent.eventType)
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
                Console.WriteLine($"Failed parsing message on Partition: {context.PartitionId}, Error: {ex.Message} - Ignoring");
            }
        }
        await context.CheckpointAsync();
    }

}
