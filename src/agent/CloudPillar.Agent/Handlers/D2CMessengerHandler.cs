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
  
    public async Task SendStreamingUploadChunkEventAsync(Stream readStream, string absolutePath, string actionId, long? startPosition = null, long? endPosition = null, CancellationToken cancellationToken = default)
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
                StartPosition = startPosition ?? 0,
                EndPosition = endPosition,
                ActionId = actionId,
                Data = data
            };

            // var messageString = JsonConvert.SerializeObject(streamingUploadChunkEvent);
            // var message = new Message(Encoding.ASCII.GetBytes(messageString)); // TODO: why ASCII?

            // // message.Properties.Add("device_id", device_id);
            // message.Properties.Add("chunk_index", chunkIndex.ToString());
            // message.Properties.Add("total_chunks", totalChunks.ToString());

            await SendMessageAsync(streamingUploadChunkEvent);
        }
    }
    private async Task SendMessageAsync(AgentEvent agentEvent)
    {
        var messageString = JsonConvert.SerializeObject(agentEvent);
        Message message = new Message(Encoding.ASCII.GetBytes(messageString));
        await _deviceClient.SendEventAsync(message);
    }
}