using System.Collections.Concurrent;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Shared.Entities.Events;


namespace CloudPillar.Agent.Handlers;

public class D2CMessengerHandler : ID2CMessengerHandler
{
    private readonly IDeviceClientWrapper _deviceClient;

    public D2CMessengerHandler(IDeviceClientWrapper deviceClientWrapper)
    {
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper)); ;
    }

    public async Task SendFirmwareUpdateEventAsync(string fileName, string actionId, long? startPosition = null, long? endPosition = null)
    {
        // Deduct the chunk size based on the protocol being used
        int chunkSize = _deviceClient.GetChunkSizeByTransportType();

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

    public async Task SendStreamingUploadChunkEventAsync(Stream readStream, string absolutePath, string actionId, string correlationId, long startPosition = 0, long? endPosition = null, CancellationToken cancellationToken = default)
    {
        // Deduct the chunk size based on the protocol being used
        int chunkSize = _deviceClient.GetChunkSizeByTransportType();
        int totalChunks = (int)Math.Ceiling((double)readStream.Length / chunkSize);

        for (int chunkIndex = (int)(startPosition / chunkSize); chunkIndex < totalChunks; chunkIndex++)
        {
            int offset = chunkIndex * chunkSize;
            int length = (chunkIndex == totalChunks - 1) ? (int)(readStream.Length - offset) : chunkSize;
            byte[] data = new byte[length];
            await readStream.ReadAsync(data, offset, length, cancellationToken);

            // Deduct the chunk size based on the protocol being used

            var streamingUploadChunkEvent = new StreamingUploadChunkEvent()
            {
                AbsolutePath = absolutePath,
                ChunkSize = chunkSize,
                StartPosition = startPosition,
                EndPosition = endPosition,
                ActionId = actionId,
                Data = data
            };

            var properties = new Dictionary<string, string>
                {
                    { "chunk_index", chunkIndex.ToString() },
                    { "total_chunks", totalChunks.ToString() },
                };

            await SendMessageAsync(streamingUploadChunkEvent, properties);
        }
    }   
    private async Task SendMessageAsync(AgentEvent agentEvent, Dictionary<string, string>? properties = null)
    {
        var messageString = JsonConvert.SerializeObject(agentEvent);
        Message message = new Message(Encoding.ASCII.GetBytes(messageString));
        if (properties != null)
        {
            foreach (var prop in properties)
            {
                message.Properties.Add(prop.Key, prop.Value);
            }
        }

        await _deviceClient.SendEventAsync(message);
    }
}