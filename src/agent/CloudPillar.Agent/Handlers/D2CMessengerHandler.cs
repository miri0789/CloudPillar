using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Messages;

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

    public async Task SendFirmwareUpdateEventAsync(CancellationToken cancellationToken, string fileName, int actionIndex, int? rangeIndex, long? startPosition = null, long? endPosition = null)
    {
        // Deduct the chunk size based on the protocol being used
        int chunkSize = _deviceClientWrapper.GetChunkSizeByTransportType();

        var firmwareUpdateEvent = new FirmwareUpdateEvent()
        {
            FileName = fileName,
            ChunkSize = chunkSize,
            RangeIndex = rangeIndex ?? 0,
            StartPosition = startPosition ?? 0,
            EndPosition = endPosition,
            ActionIndex = actionIndex
        };

        await SendMessageAsync(firmwareUpdateEvent, cancellationToken);
    }

    public async Task SendStreamingUploadChunkEventAsync(byte[] buffer, Uri storageUri, long currentPosition, string checkSum, CancellationToken cancellationToken, bool isRunDiagnostic = false)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            var streamingUploadChunkEvent = new StreamingUploadChunkEvent()
            {
                StorageUri = storageUri,
                CheckSum = checkSum,
                StartPosition = currentPosition,
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
            Data = certificate.Export(X509ContentType.Cert)
        };

        await SendMessageAsync(ProvisionDeviceCertificateEvent, cancellationToken);
    }

    public async Task SendSignFileEventAsync(SignFileEvent d2CMessage, CancellationToken cancellationToken)
    {
        await SendMessageAsync(d2CMessage, cancellationToken);
    }

    public async Task SendSignTwinKeyEventAsync(string keyPath, string signatureKey, CancellationToken cancellationToken)
    {
        await SendMessageAsync(new SignEvent(), cancellationToken);
    }

    private async Task SendMessageAsync(D2CMessage d2CMessage, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            Message message = PrepareD2CMessage(d2CMessage);
            await _deviceClientWrapper.SendEventAsync(message, cancellationToken);
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