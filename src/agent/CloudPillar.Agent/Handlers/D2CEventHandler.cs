using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using shared.Entities.Events;
using CloudPillar.Agent.Interfaces;

namespace CloudPillar.Agent.Handlers;

public class D2CEventHandler : ID2CEventHandler
{
    private readonly ICommonHandler _commonHandler;
    private readonly DeviceClient _deviceClient;

    private const int kB = 1024;
    public D2CEventHandler(ICommonHandler commonHandler, IEnvironmentsWrapper environmentsWrapper)
    {
        ArgumentNullException.ThrowIfNull(commonHandler);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        _commonHandler = commonHandler;
        string _deviceConnectionString = environmentsWrapper.deviceConnectionString;
        TransportType _transportType = commonHandler.GetTransportType();
        _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, _transportType);
    }

    public async Task SendFirmwareUpdateEventAsync(string fileName, Guid actionGuid, long? startPosition = null, long? endPosition = null)
    {
        // Deduct the chunk size based on the protocol being used
        int chunkSize = _commonHandler.GetTransportType() switch
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