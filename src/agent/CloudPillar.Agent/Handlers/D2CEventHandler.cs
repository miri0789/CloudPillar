using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using shared.Entities.Events;

namespace CloudPillar.Agent.Handlers
{
    public interface ID2CEventHandler
    {
        Task SendFirmwareUpdateEventAsync(string fileName, Guid actionGuid, long? startPosition = null, long? endPosition = null);
    }

    public class D2CEventHandler : ID2CEventHandler
    {
        private readonly ICommonHandler _commonHandler;
        private readonly DeviceClient _deviceClient;
        public D2CEventHandler(ICommonHandler commonHandler)
        {
            _commonHandler = commonHandler;
            string _deviceConnectionString = Environment.GetEnvironmentVariable("DEVICE_CONNECTION_STRING");
            TransportType _transportType = commonHandler.GetTransportType();
            _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, _transportType);
        }

        public async Task SendFirmwareUpdateEventAsync(string fileName, Guid actionGuid, long? startPosition = null, long? endPosition = null)
        {
            // Deduct the chunk size based on the protocol being used
            int chunkSize = _commonHandler.GetTransportType() switch
            {
                TransportType.Mqtt => 32 * 1024, // 32 KB
                TransportType.Amqp => 64 * 1024, // 64 KB
                TransportType.Http1 => 256 * 1024, // 256 KB
                _ => 32 * 1024 // 32 KB (default)
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
}