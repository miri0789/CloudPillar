using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json.Linq;
using shared.Entities.Messages;
using CloudPillar.Agent.Interfaces;

namespace CloudPillar.Agent.Handlers;

/// <summary>
/// Represents a subscription to Cloud-to-Device messages for a specific device client.
/// </summary>
public class C2DSubscriptionHandler : IC2DSubscriptionHandler
{
    private readonly DeviceClient _deviceClient;
    private CancellationTokenSource _privateCts;
    private Task _c2dTask;
    private readonly string _deviceId;

    private readonly IFileDownloadHandler _fileDownloadHandler;

    /// <summary>
    /// Gets a value indicating whether the subscription is active.
    /// </summary>
    private bool _isSubscribed { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="C2DSubscription"/> class.
    /// </summary>
    /// <param name="deviceClient">The DeviceClient associated with the device.</param>
    /// <param name="deviceId">The device identifier.</param>
    public C2DSubscriptionHandler(ICommonHandler commonHandler, IFileDownloadHandler fileDownloadHandler)
    {
        ArgumentNullException.ThrowIfNull(commonHandler);
        ArgumentNullException.ThrowIfNull(fileDownloadHandler);

        _fileDownloadHandler = fileDownloadHandler;
        string _deviceConnectionString = Environment.GetEnvironmentVariable("DEVICE_CONNECTION_STRING");
        TransportType _transportType = commonHandler.GetTransportType();
        _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, _transportType);
        _deviceId = commonHandler.GetDeviceIdFromConnectionString(_deviceConnectionString);
    }

    /// <summary>
    /// Subscribes to Cloud-to-Device messages.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous subscribe operation.</returns>
    public async Task Subscribe(CancellationToken cancellationToken)
    {
        if (_privateCts != null)
        {
            Console.WriteLine("Already subscribed to C2D messages.");
            return;
        }

        _privateCts = new CancellationTokenSource();
        CancellationToken combinedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _privateCts.Token).Token;
        Console.WriteLine("Subscribing to C2D messages...");

        _isSubscribed = true;
        _c2dTask = Task.Run(() => ReceiveCloudToDeviceMessagesAsync(combinedToken, _deviceId), combinedToken);
    }

    /// <summary>
    /// Unsubscribes from Cloud-to-Device messages.
    /// </summary>
    public void Unsubscribe()
    {
        if (_privateCts == null)
        {
            Console.WriteLine("Not subscribed to C2D messages.");
            return;
        }

        Console.WriteLine("Unsubscribing from C2D messages...");
        _privateCts.Cancel();
        _privateCts = null;
        _isSubscribed = false;
    }

    public bool CheckSubscribed()
    {
        return _isSubscribed;
    }

    /// <summary>
    /// Receives and processes Cloud-to-Device messages.
    /// </summary>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="device_id">The device identifier.</param>
    /// <returns>A task that represents the asynchronous message receiving operation.</returns>
    private async Task ReceiveCloudToDeviceMessagesAsync(CancellationToken cancellationToken, string device_id)
    {
        Console.WriteLine($"Started listening for C2D messages at device '{device_id}'....");
        // long totalBytesDownloaded = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            Message receivedMessage = null;

            try
            {
                receivedMessage = await _deviceClient.ReceiveAsync(cancellationToken);

                try
                {
                    string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    MessageType.TryParse(receivedMessage.Properties["MessageType"], out MessageType messageType);

                    switch (messageType)
                    {
                        case MessageType.DownloadChunk:
                            DownloadBlobChunkMessage downloadBlobChunkMessage = new DownloadBlobChunkMessage();
                            downloadBlobChunkMessage.CreateObjectFromMessage<DownloadBlobChunkMessage>(receivedMessage);
                            _fileDownloadHandler.DownloadMessageDataAsync(downloadBlobChunkMessage, receivedMessage.GetBytes());
                            break;
                        default: break;
                    }

                    await _deviceClient.CompleteAsync(receivedMessage);
                    Console.WriteLine($"{0}: Recived message of type: {1} completed", DateTime.Now, messageType.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{0}: Exception hit when parsing the message, ignoring it: {1}", DateTime.Now, ex.Message);
                    continue;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{0}: Exception hit when receiving the message, ignoring it: {1}", DateTime.Now, ex.Message);
                continue;
            }
        }
    }
}