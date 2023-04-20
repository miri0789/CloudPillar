using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json.Linq;

namespace FirmwareUpdateAgent
{
    /// <summary>
    /// Represents a subscription to Cloud-to-Device messages for a specific device client.
    /// </summary>
    partial class Program
    {
        public class C2DSubscription
        {
            private readonly DeviceClient _deviceClient;
            private CancellationTokenSource _privateCts;
            private Task _c2dTask;
            private readonly string _deviceId;

            /// <summary>
            /// Gets a value indicating whether the subscription is active.
            /// </summary>
            public bool IsSubscribed { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="C2DSubscription"/> class.
            /// </summary>
            /// <param name="deviceClient">The DeviceClient associated with the device.</param>
            /// <param name="deviceId">The device identifier.</param>
            public C2DSubscription(DeviceClient deviceClient, string deviceId)
            {
                _deviceClient = deviceClient;
                _deviceId = deviceId;
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

                IsSubscribed = true;
                _c2dTask = Task.Run(() => ReceiveCloudToDeviceMessages(combinedToken, _deviceId), combinedToken);
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
                IsSubscribed = false;
            }

            /// <summary>
            /// Receives and processes Cloud-to-Device messages.
            /// </summary>
            /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
            /// <param name="device_id">The device identifier.</param>
            /// <returns>A task that represents the asynchronous message receiving operation.</returns>
            private async Task ReceiveCloudToDeviceMessages(CancellationToken cancellationToken, string device_id)
            {
                Console.WriteLine($"Started listening for C2D messages at device '{device_id}'....");
                long totalBytesDownloaded = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    Message receivedMessage = null;
                    try
                    {
                        receivedMessage = await _deviceClient.ReceiveAsync(cancellationToken);
                    }
                    catch (Exception x)
                    {
                        Console.WriteLine("{0}: Exception hit when receiving the message, ignoring it: {1}", DateTime.Now, x.Message);
                        continue;
                    }

                    if (receivedMessage != null)
                    {
                        string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());

                        // Read properties from the received message
                        int chunkIndex = int.Parse(receivedMessage.Properties["chunk_index"]);
                        int totalChunks = int.Parse(receivedMessage.Properties["total_chunks"]);

                        try
                        {
                            JObject messageObject = JObject.Parse(messageData);

                            string filename = messageObject.Value<string>("filename");
                            int writePosition = messageObject.Value<int>("write_position");
                            string uuencodedData = messageObject.Value<string>("data");

                            // Decode the uuencoded data and write it to the file
                            byte[] bytes = Convert.FromBase64String(uuencodedData);
                            totalBytesDownloaded = await WriteChunkToFile(filename, writePosition, bytes, _stopwatch, totalBytesDownloaded, 100 * (chunkIndex + 1) / totalChunks);

                            // Mark the message as completed to remove it from the queue
                            await _deviceClient.CompleteAsync(receivedMessage);
                        }
                        catch (Exception x)
                        {
                            Console.WriteLine("{0}: Exception hit when parsing the message, ignoring it: {1}", DateTime.Now, x.Message);
                            continue;
                        }
                    }
                }
            }
        }
    }
}
