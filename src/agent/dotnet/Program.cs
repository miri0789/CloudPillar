using System.Net;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace FirmwareUpdateAgent
{
    class Program
    {
        private static DeviceClient _deviceClient;
        private static string _deviceConnectionString;
        private static CancellationTokenSource _cts;
        private static Mutex _mutex;
        private static HttpListener _httpListener;
        private static bool _isPaused = false;
        private static Stopwatch _stopwatch = new Stopwatch();

        static async Task Main(string[] args)
        {
            _deviceConnectionString = Environment.GetEnvironmentVariable("DEVICE_CONNECTION_STRING");

            if (string.IsNullOrEmpty(_deviceConnectionString))
            {
                Console.WriteLine("DEVICE_CONNECTION_STRING environment variable is missing.");
                return;
            }

            bool createdNew;
            using (var mutex = new Mutex(true, "IoTDeviceAgentMutex", out createdNew))
            {
                try
                {
                    if (!createdNew)
                    {
                        Console.WriteLine("Another instance of FirmwareUpdateAgent is already running.");
                        return;
                    }

                    _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, GetTransportType());

                    _cts = new CancellationTokenSource();
                    // var d2cTask = Task.Run(() => SendFirmwareUpdateReady(cts.Token, GetDeviceIdFromConnectionString(deviceConnectionString)), cts.Token);
                    var c2dTask = Task.Run(() => ReceiveCloudToDeviceMessages(_cts.Token), _cts.Token);

                    _httpListener = new HttpListener();
                    _httpListener.Prefixes.Add("http://localhost:8099/");
                    _httpListener.Start();
                    var httpTask = Task.Run(() => HandleHttpListener(_cts.Token), _cts.Token);

                    await Task.WhenAny(/*d2cTask,*/ c2dTask, httpTask);

                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();

                    _cts.Cancel();

                    _httpListener.Stop();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    _deviceClient?.Dispose(); _deviceClient = null;
                    mutex?.ReleaseMutex();
                }
            }
        }

        private static TransportType GetTransportType()
        {
            var transportTypeString = Environment.GetEnvironmentVariable("TRANSPORT_TYPE");
            return Enum.TryParse(transportTypeString, out TransportType transportType)
                ? transportType
                : TransportType.Mqtt;
        }

        private static async Task SendFirmwareUpdateReady(CancellationToken cancellationToken, string device_id)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_isPaused)
                {
                    // Deduct the chunk size based on the protocol being used
                    int chunkSize = GetTransportType() switch
                    {
                        TransportType.Mqtt => 32 * 1024, // 32 KB
                        TransportType.Amqp => 64 * 1024, // 64 KB
                        TransportType.Http1 => 256 * 1024, // 256 KB
                        _ => 32 * 1024 // 32 KB (default)
                    };

                    var payloadData = new
                    {
                        event_type = "FirmwareUpdateReady",
                        filename = "Microsoft Azure Storage Explorer.app.zip",
                        chunk_size = chunkSize,
                    };

                    var messageString = JsonConvert.SerializeObject(payloadData);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));

                    message.Properties.Add("device_id", device_id);

                    // Set the ExpiryTimeUtc property
                    // message.ExpiryTimeUtc = DateTime.UtcNow.AddHours(1); // 1 hour TTL

                    await _deviceClient.SendEventAsync(message);

                    Console.WriteLine("{0}: FirmwareUpdateReady sent", DateTime.Now);
                    _stopwatch.Start();
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
            }
        }

        private static async Task ReceiveCloudToDeviceMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Message receivedMessage = await _deviceClient.ReceiveAsync(TimeSpan.FromSeconds(1));

                if (receivedMessage != null && !_isPaused)
                {
                    string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    JObject messageObject = JObject.Parse(messageData);

                    string filename = messageObject.Value<string>("filename");
                    // int chunkIndex = messageObject.Value<int>("chunk_index");
                    int writePosition = messageObject.Value<int>("write_position");
                    // int totalChunks = messageObject.Value<int>("total_chunks");
                    string uuencodedData = messageObject.Value<string>("data");

                    // byte[] bytes = StringToByteArray(data);
                    // string uuencodedData = messagePayload["data"].ToString();
                    byte[] bytes = Convert.FromBase64String(uuencodedData);
                    await WriteChunkToFile(filename, writePosition, bytes, _stopwatch);

                    // Console.WriteLine("{0}: Received chunk {1} of {2} for file {3}", DateTime.Now, chunkIndex, totalChunks, filename);

                    await _deviceClient.CompleteAsync(receivedMessage); // Removes from the queue
                }
            }
        }

        private static async Task WriteChunkToFile(string filename, int writePosition, byte[] bytes, Stopwatch stopwatch, int writtenAmount = -1)
        {
            if(writtenAmount < 0) writtenAmount = writePosition;
            string path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            using (FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) // Use FileShare.Write for shared access (worker threads?)
            {
                // fileStream.Position = chunkIndex * bytes.Length;
                fileStream.Seek(writePosition, SeekOrigin.Begin);
                // fileStream.Write(bytes, 0, bytes.Length);
                await fileStream.WriteAsync(bytes, 0, bytes.Length);
            }
            double timeElapsedInSeconds = stopwatch.Elapsed.TotalSeconds;
            long totalBytesDownloaded = writePosition + bytes.Length;
            double throughput = totalBytesDownloaded / timeElapsedInSeconds / 1024.0; // in KiB/s

            Console.WriteLine($"@pos: {writePosition:00000000000} tot: {writtenAmount:00000000000} Throughput: {throughput:0.00} KiB/s");
       }

        private static byte[] StringToByteArray(string hex)
        {
            int length = hex.Length;
            byte[] bytes = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        private static async Task HandleHttpListener(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await _httpListener.GetContextAsync();
                HttpListenerRequest request = context.Request;

                if (request.HttpMethod == "GET")
                {
                    if (request.Url.AbsolutePath.ToLower() == "/pause")
                    {
                        _isPaused = true;
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/start")
                    {
                        _isPaused = false;
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/update")
                    {
                        SendFirmwareUpdateReady(_cts.Token, GetDeviceIdFromConnectionString(_deviceConnectionString)); // Async call for update
                    }
                }

                using HttpListenerResponse response = context.Response;
                string responseString = _isPaused ? "Agent is paused" : "Agent is running";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        private static string GetDeviceIdFromConnectionString(string connectionString)
        {
            var items = connectionString.Split(';');
            foreach (var item in items)
            {
                if (item.StartsWith("DeviceId"))
                {
                    return item.Split('=')[1];
                }
            }

            throw new ArgumentException("DeviceId not found in the connection string.");
        }

    }
}
