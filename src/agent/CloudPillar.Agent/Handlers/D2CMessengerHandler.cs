using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Shared.Entities.Messages;


namespace CloudPillar.Agent.Handlers;

public class D2CMessengerHandler : ID2CMessengerHandler
{
    private readonly IDeviceClientWrapper _deviceClient;

    private const int kB = 1024;
    public D2CMessengerHandler(IDeviceClientWrapper deviceClientWrapper)
    {
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));;
    }

    public async Task SendFirmwareUpdateEventAsync(string fileName, string actionId, long? startPosition = null, long? endPosition = null)
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
            ActionId = actionId
        };

        await SendMessageAsync(firmwareUpdateEvent);
    }

    private async Task SendMessageAsync(D2CMessage agentEvent)
    {
        var messageString = JsonConvert.SerializeObject(agentEvent);
        Message message = new Message(Encoding.ASCII.GetBytes(messageString));
        await _deviceClient.SendEventAsync(message);
    }
}