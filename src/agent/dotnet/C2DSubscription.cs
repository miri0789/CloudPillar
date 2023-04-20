using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json.Linq;

namespace FirmwareUpdateAgent
{
    partial class Program
    {
        public class C2DSubscription
        {
            private readonly DeviceClient _deviceClient;
            private CancellationTokenSource _privateCts;
            private Task _c2dTask;
            private readonly string _deviceId;
            public bool IsSubscribed { get; private set; }

            public C2DSubscription(DeviceClient deviceClient, string deviceId)
            {
                _deviceClient = deviceClient;
                _deviceId = deviceId;
            }

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

            private async Task ReceiveCloudToDeviceMessages(CancellationToken cancellationToken, string device_id)
            {
                Console.WriteLine($"Started listening for C2D messages at device '{device_id}'....");
                long totalBytesDownloaded = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    Message receivedMessage = null;
                    try
                    {
                        receivedMessage = await _deviceClient.ReceiveAsync(cancellationToken);//TimeSpan.FromSeconds(1));
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

                            byte[] bytes = Convert.FromBase64String(uuencodedData);
                            totalBytesDownloaded = await WriteChunkToFile(filename, writePosition, bytes, _stopwatch, totalBytesDownloaded, 100 * (chunkIndex+1) / totalChunks);

                            await _deviceClient.CompleteAsync(receivedMessage); // Removes from the queue
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
