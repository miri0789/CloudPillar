using System.Text;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using shared.Entities.Messages;


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
    private readonly IDeviceClientWrapper _deviceClientWrapper;

    /// <summary>
    /// Gets a value indicating whether the subscription is active.
    /// </summary>
    private bool _isSubscribed { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="C2DSubscription"/> class.
    /// </summary>
    /// <param name="deviceClient">The DeviceClient associated with the device.</param>
    /// <param name="deviceId">The device identifier.</param>
    public C2DSubscriptionHandler(IDeviceClientWrapper deviceClientWrapper, IFileDownloadHandler fileDownloadHandler)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(fileDownloadHandler);

        _fileDownloadHandler = fileDownloadHandler;
        _deviceClient = deviceClientWrapper.CreateDeviceClient();
        _deviceId = deviceClientWrapper.GetDeviceId();
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

        while (!cancellationToken.IsCancellationRequested)
        {
            Message receivedMessage;

            try
            {
                receivedMessage = await _deviceClientWrapper.ReceiveAsync(cancellationToken,_deviceClient);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: Exception hit when receiving the message, ignoring it: {ex.Message}");
                continue;
            }

            try
            {
                string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                MessageType.TryParse(receivedMessage.Properties["MessageType"], out MessageType messageType);

                switch (messageType)
                {
                    case MessageType.DownloadChunk:
                        DownloadBlobChunkMessage downloadBlobChunkMessage = new DownloadBlobChunkMessage(receivedMessage);
                        _fileDownloadHandler.DownloadMessageDataAsync(downloadBlobChunkMessage);
                        break;
                    
                    default: 
                        Console.WriteLine($"Recived message was not processed"); 
                        break;
                }

                await _deviceClient.CompleteAsync(receivedMessage);
                Console.WriteLine($"{DateTime.Now}: Recived message of type: {messageType.ToString()} completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now}: Exception hit when parsing the message, ignoring it: {ex.Message}");
                continue;
            }
        }
    }
}