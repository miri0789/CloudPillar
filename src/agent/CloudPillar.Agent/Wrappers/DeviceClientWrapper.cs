using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using Newtonsoft.Json.Linq;
using CloudPillar.Agent.Entities;

namespace CloudPillar.Agent.Wrappers;
public class DeviceClientWrapper : IDeviceClientWrapper
{
    private DeviceClient _deviceClient;
    private readonly AuthenticationSettings _authenticationSettings;
    private readonly ILoggerHandler _logger;
    private const int kB = 1024;

    /// <summary>
    /// Initializes a new instance of the DeviceClient class
    /// </summary>
    /// <param name="environmentsWrapper"></param>
    /// <exception cref="NullReferenceException"></exception>
    public DeviceClientWrapper(IOptions<AuthenticationSettings> authenticationSettings, ILoggerHandler logger)
    {
        _authenticationSettings = authenticationSettings?.Value ?? throw new ArgumentNullException(nameof(authenticationSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> DeviceInitializationAsync(string hostname, IAuthenticationMethod authenticationMethod, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(hostname);
        ArgumentNullException.ThrowIfNull(authenticationMethod);
        try
        {
            var iotClient = DeviceClient.Create(hostname, authenticationMethod, GetTransportSettings());
            if (iotClient != null)
            {
                // iotClient never returns null also if the device does not exist, so to check if the device exists or the certificate is valid we try to get the device twin.
                var twin = await iotClient.GetTwinAsync(cancellationToken);
                if (twin != null)
                {
                    _deviceClient = iotClient;
                    return true;
                }
                else
                {
                    _logger.Info($"Device does not exist in {hostname}.");
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"DeviceInitializationAsync, Exception: {ex.Message}");
            return false;
        }
    }

    public ITransportSettings[] GetTransportSettings()
    {
        var transportType = GetTransportType();
        switch (transportType)
        {
            case TransportType.Mqtt:
            case TransportType.Mqtt_Tcp_Only:
            case TransportType.Mqtt_WebSocket_Only:
                return new ITransportSettings[]{
                    new MqttTransportSettings(transportType){
                        RemoteCertificateValidationCallback = ValidateCertificate}};
            case TransportType.Amqp:
            case TransportType.Amqp_Tcp_Only:
                return new ITransportSettings[]{
                    new AmqpTransportSettings(TransportType.Amqp_Tcp_Only)
                        {RemoteCertificateValidationCallback = ValidateCertificate}};
            case TransportType.Amqp_WebSocket_Only:
                return new ITransportSettings[]{
                    new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only)
                        {RemoteCertificateValidationCallback = ValidateCertificate}};
            case TransportType.Http1:
            default:
                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = ValidateCertificate
                };
                return new ITransportSettings[]
                {
                    new Http1TransportSettings()
                        {
                            HttpClient = new HttpClient(httpClientHandler)
                        }
                };

        }
    }

    public TransportType GetTransportType()
    {
        var transportTypeString = _authenticationSettings.TransportType;
        return Enum.TryParse(transportTypeString, out TransportType transportType)
            ? transportType
            : TransportType.Amqp;
    }

    public bool ValidateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        if (sender is SslStream sslSender)
        {
            var targetHostName = sslSender.TargetHostName?.ToString();
            return _authenticationSettings.ValidDomains.Contains(targetHostName);
        }

        return false;
    }


    public async Task<DeviceConnectResultEnum> IsDeviceInitializedAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Check if the device is already initialized
            await GetTwinAsync(cancellationToken);
            return DeviceConnectResultEnum.Valid;
        }
        catch (Exception ex)
        {
            JObject exceptionData = JObject.Parse(ex.Message);
            var error = exceptionData?["errorCode"]?.ToString();
            if (error is not null && Enum.TryParse(error, out DeviceConnectResultEnum errorCode))
            {
                return errorCode;
            }
            // Extract the error code
            _logger.Debug($"IsDeviceInitializedAsync, Device is not initialized. {ex.Message}");
            return DeviceConnectResultEnum.Unknow;
        }
    }
    public ProvisioningTransportHandler GetProvisioningTransportHandler()
    {
        return GetTransportType() switch
        {
            TransportType.Mqtt => new ProvisioningTransportHandlerMqtt(),
            TransportType.Mqtt_Tcp_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.TcpOnly),
            TransportType.Mqtt_WebSocket_Only => new ProvisioningTransportHandlerMqtt(TransportFallbackType.WebSocketOnly),
            TransportType.Amqp => new ProvisioningTransportHandlerAmqp(),
            TransportType.Amqp_Tcp_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.TcpOnly),
            TransportType.Amqp_WebSocket_Only => new ProvisioningTransportHandlerAmqp(TransportFallbackType.WebSocketOnly),
            TransportType.Http1 => new ProvisioningTransportHandlerHttp(),
        };
    }



    public int GetChunkSizeByTransportType()
    {
        int chunkSize =
        GetTransportType() switch
        {
            TransportType.Mqtt => 32 * kB,
            TransportType.Amqp => 64 * kB,
            TransportType.Http1 => 256 * kB,
            _ => 32 * kB
        };
        return chunkSize;
    }
    /// <summary>
    /// Asynchronously waits for a message to be received from the device.
    /// after recived the message, need to exec CompleteAsync function to the message
    /// </summary>
    /// <param name="cancellationToken">used to cancel the operation if needed.</param>
    /// <returns>a task that represents the asynchronous operation and contains a Message when received.</returns>
    public Task<Message> ReceiveAsync(CancellationToken cancellationToken)
    {
        return _deviceClient.ReceiveAsync(cancellationToken);
    }

    /// <summary>
    /// Asynchronously sends an event message to the device.
    /// </summary>
    /// <param name="message">The message object containing the data to be sent.</param>
    /// <returns>a task representing the asynchronous operation.</returns>
    public async Task SendEventAsync(Message message, CancellationToken cancellationToken)
    {
        await _deviceClient.SendEventAsync(message, cancellationToken);
    }

    /// <summary>
    /// asynchronously completes the processing of a received message.
    /// </summary>
    /// <param name="message">the message object representing the received message to be completed.</param>
    /// <returns>a task representing the asynchronous operation.</returns>
    public async Task CompleteAsync(Message message, CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            await _deviceClient.CompleteAsync(message, cancellationToken);
        }
    }

    public async Task DisposeAsync()
    {
        if (_deviceClient != null)
        {
            await _deviceClient.DisposeAsync();
        }
    }

    public async Task<Twin> GetTwinAsync(CancellationToken cancellationToken)
    {
        var twin = await _deviceClient.GetTwinAsync(cancellationToken);
        return twin;
    }

    public async Task UpdateReportedPropertiesAsync(string key, object? value, CancellationToken cancellationToken)
    {
        var updatedReportedProperties = new TwinCollection();
        updatedReportedProperties[char.ToLower(key[0]) + key.Substring(1)] = value;
        await _deviceClient.UpdateReportedPropertiesAsync(updatedReportedProperties, cancellationToken);
    }

    public async Task UpdateReportedPropertiesAsync(TwinCollection reportedProperties, CancellationToken cancellationToken)
    {
        await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties, cancellationToken);
    }

    public async Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(FileUploadSasUriRequest request, CancellationToken cancellationToken = default)
    {
        return await _deviceClient.GetFileUploadSasUriAsync(request, cancellationToken);
    }

    public async Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken = default)
    {
        await _deviceClient.CompleteFileUploadAsync(notification, cancellationToken);
    }
    public async Task CompleteFileUploadAsync(string correlationId, bool isSuccess, CancellationToken cancellationToken = default)
    {
        FileUploadCompletionNotification notification = new FileUploadCompletionNotification
        {
            CorrelationId = correlationId,
            IsSuccess = isSuccess
        };
        await _deviceClient.CompleteFileUploadAsync(notification, cancellationToken);
    }

    public Uri GetBlobUri(FileUploadSasUriResponse sasUri)
    {
        return sasUri.GetBlobUri();
    }

    public async Task SetDesiredPropertyUpdateCallbackAsync(DesiredPropertyUpdateCallback callback, CancellationToken cancellationToken = default)
    {
        await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(callback, null, cancellationToken);
    }
}
