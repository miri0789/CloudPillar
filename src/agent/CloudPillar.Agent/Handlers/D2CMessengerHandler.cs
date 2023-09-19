using System.Reflection;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Shared.Entities.Factories;
using Shared.Entities.Messages;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class D2CMessengerHandler : ID2CMessengerHandler
{
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly IMessageFactory _messageFactory;
    private readonly ILoggerHandler _logger;

    public D2CMessengerHandler(IDeviceClientWrapper deviceClientWrapper, IMessageFactory messageFactory, ILoggerHandler logger)
    {
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _messageFactory = messageFactory ?? throw new ArgumentNullException(nameof(messageFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger)); ;
    }

    public async Task SendFirmwareUpdateEventAsync(string fileName, string actionId, long? startPosition = null, long? endPosition = null)
    {
        // Deduct the chunk size based on the protocol being used
        int chunkSize = _deviceClientWrapper.GetChunkSizeByTransportType();

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

    public async Task SendStreamingUploadChunkEventAsync(byte[] buffer, Uri storageUri, string actionId, long currentPosition)
    {
        var streamingUploadChunkEvent = new StreamingUploadChunkEvent()
        {
            StorageUri = storageUri,
            CheckSum = "",
            StartPosition = currentPosition,
            ActionId = actionId ?? Guid.NewGuid().ToString(),
            Data = buffer
        };

        await SendMessageAsync(streamingUploadChunkEvent);
    }



    private async Task SendMessageAsync(D2CMessage d2CMessage)
    {
        Message message = PrepareD2CMessage(d2CMessage);
        await _deviceClientWrapper.SendEventAsync(message);
    }

    private Message PrepareD2CMessage(D2CMessage d2CMessage)
    {
        var messageString = JsonConvert.SerializeObject(d2CMessage);
        Message message = new Message(Encoding.ASCII.GetBytes(messageString));

        PropertyInfo[] properties = d2CMessage.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (property.Name != "Data")
            {
                message.Properties.Add(property.Name, property.GetValue(d2CMessage)?.ToString());
            }
        }
        return message;
    }
}