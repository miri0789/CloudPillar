using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Shared.Entities.Messages;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class D2CMessengerHandler : ID2CMessengerHandler
{
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly ILoggerHandler _logger;

    public D2CMessengerHandler(IDeviceClientWrapper deviceClientWrapper, ILoggerHandler logger)
    {
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendFirmwareUpdateEventAsync(CancellationToken cancellationToken, string fileName, string actionId, long? startPosition = null, long? endPosition = null)
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

        await SendMessageAsync(firmwareUpdateEvent, cancellationToken);
    }

    public async Task SendStreamingUploadChunkEventAsync(byte[] buffer, Uri storageUri, string actionId, long currentPosition, string checkSum, CancellationToken cancellationToken, bool isRunDiagnostic = false)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            var streamingUploadChunkEvent = new StreamingUploadChunkEvent()
            {
                StorageUri = storageUri,
                CheckSum = checkSum,
                StartPosition = currentPosition,
                ActionId = actionId ?? Guid.NewGuid().ToString(),
                Data = buffer,
                IsRunDiagnostics = isRunDiagnostic
            };

            await SendMessageAsync(streamingUploadChunkEvent, cancellationToken);
        }
    }

    public async Task ProvisionDeviceCertificateEventAsync(X509Certificate2 certificate, CancellationToken cancellationToken)
    {
        var ProvisionDeviceCertificateEvent = new ProvisionDeviceCertificateEvent()
        {
            Data = certificate.Export(X509ContentType.Cert),
            ActionId = Guid.NewGuid().ToString()
        };

        await SendMessageAsync(ProvisionDeviceCertificateEvent, cancellationToken);
    }

    private async Task SendMessageAsync(D2CMessage d2CMessage, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            Message message = PrepareD2CMessage(d2CMessage);
            await _deviceClientWrapper.SendEventAsync(message);
        }
    }

    private Message PrepareD2CMessage(D2CMessage d2CMessage)
    {
        var messageString = JsonConvert.SerializeObject(d2CMessage);
        Message message = new Message(Encoding.UTF8.GetBytes(messageString));

        PropertyInfo[] properties = d2CMessage.GetType().GetProperties();
        foreach (var property in properties)
        {
            if (property.Name != "Data")
            {
                message.Properties.Add(property.Name, property.GetValue(d2CMessage)?.ToString());
            }
        }
        _logger.Debug($"D2CMessengerHandler PrepareD2CMessage. message title: {d2CMessage.MessageType.ToString()}, properties: {string.Join(Environment.NewLine, message.Properties)}");
        return message;
    }

}