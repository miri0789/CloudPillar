using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Shared.Entities.Events;


namespace CloudPillar.Agent.Handlers;

public class D2CMessengerHandler : ID2CMessengerHandler
{
    private readonly IDeviceClientWrapper _deviceClient;

    private const int kB = 1024;
    public D2CMessengerHandler(IDeviceClientWrapper deviceClientWrapper, IEnvironmentsWrapper environmentsWrapper)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        _deviceClient = deviceClientWrapper;
    }

    public async Task SendFirmwareUpdateEventAsync(string fileName, Guid actionGuid, long? startPosition = null, long? endPosition = null)
    {
        // Deduct the chunk size based on the protocol being used
        int chunkSize = _deviceClient.GetTransportType() switch
        {
            TransportType.Mqtt => 32 * kB,
            TransportType.Amqp => 64 * kB,
            TransportType.Http1 => 256 * kB,
            _ => 32 * kB
        };

        var firmwareUpdateEvent = new FirmwareUpdateEvent()
        {
            FileName = fileName,
            ChunkSize = chunkSize,
            StartPosition = startPosition ?? 0,
            EndPosition = endPosition,
            ActionGuid = actionGuid
        };

        await SendMessageAsync(firmwareUpdateEvent);
    }

    private async Task SendMessageAsync(AgentEvent agentEvent)
    {
        var messageString = JsonConvert.SerializeObject(agentEvent);
        Message message = new Message(Encoding.ASCII.GetBytes(messageString));
        await _deviceClient.SendEventAsync(message);
    }
}