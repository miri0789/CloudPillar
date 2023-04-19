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
        private static Mutex _mutex;
        private static HttpListener _httpListener;
        private static bool _isPaused = false;
        private static Stopwatch _stopwatch = new Stopwatch();
        private static bool _isShutdown = false;

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
                    CancellationTokenSource cts = null;
                    while(!_isShutdown) {
                        try {
                            cts = new CancellationTokenSource();
                            Console.WriteLine("Loading the Device Client....");
                            _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, GetTransportType());

                            Console.WriteLine("Constructing c2d listener....");
                            // var d2cTask = Task.Run(() => SendFirmwareUpdateReady(cts.Token, GetDeviceIdFromConnectionString(deviceConnectionString)), cts.Token);
                            var c2dTask = Task.Run(() => ReceiveCloudToDeviceMessages(cts.Token, GetDeviceIdFromConnectionString(_deviceConnectionString)), cts.Token);

                            Console.WriteLine("Constructing HTTP Listener....");
                            _httpListener = new HttpListener();
                            _httpListener.Prefixes.Add("http://+:8099/");
                            _httpListener.Start();
                            var httpTask = Task.Run(() => HandleHttpListener(cts.Token), cts.Token);

                            Console.WriteLine("Starting Listeners....");
                            await Task.WhenAny(/*d2cTask,*/ c2dTask, httpTask);
                            Console.WriteLine("Bailed out of Listeners, waiting for cancellation token....");

                            await cts.Token;
                            Console.WriteLine("Cancellation done!");

                            // Console.WriteLine("Press any key to exit...");
                            // Console.ReadKey();

                        } finally {
                            cts?.Cancel();
                            Console.WriteLine("Bailed out of the Agent....");
                            _httpListener.Stop(); _httpListener = null;
                            _stopwatch.Reset(); // Resetting stopwatch causes next throughput calculations to reset
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    _deviceClient?.Dispose(); _deviceClient = null;
                    if (createdNew)
                    {
                        try {
                            mutex?.ReleaseMutex();
                        } catch(Exception ) {}
                    }
                }
            }
        }

        private static TransportType GetTransportType()
        {
            var transportTypeString = Environment.GetEnvironmentVariable("TRANSPORT_TYPE");
            return Enum.TryParse(transportTypeString, out TransportType transportType)
                ? transportType
                : TransportType.Amqp;
        }

        private static async Task SendFirmwareUpdateReadyContd(CancellationToken cancellationToken, string device_id, string filename, int fromPos = -1)
        {
            Console.WriteLine($"SDK command 'Continue' at device '{device_id}'....");
            string path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            FileInfo fi = new FileInfo(path);
            await SendFirmwareUpdateReady(cancellationToken, device_id, filename, fromPos >= 0 ? fromPos : fi.Exists ? fi.Length : 0L);
        }
        private static async Task SendFirmwareUpdateReady(CancellationToken cancellationToken, string device_id, string filename, long startFromPos = -1L)
        {
            Console.WriteLine($"Sending FirmwareUpdateReady event at device '{device_id}'....");
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
                        filename = filename,
                        chunk_size = chunkSize,
                        start_from = startFromPos >= 0 ? startFromPos : 0,
                    };

                    var messageString = JsonConvert.SerializeObject(payloadData);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));

                    message.Properties.Add("device_id", device_id);

                    // Set the ExpiryTimeUtc property
                    // message.ExpiryTimeUtc = DateTime.UtcNow.AddHours(1); // 1 hour TTL

                    await _deviceClient.SendEventAsync(message);

                    Console.WriteLine("{0}: FirmwareUpdateReady sent", DateTime.Now);
                    // _stopwatch.Start();
                    break;
                } else {
                    _stopwatch.Reset(); // Resetting stopwatch causes next throughput calculations to reset
                }

                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
            }
        }

        private static async Task ReceiveCloudToDeviceMessages(CancellationToken cancellationToken, string device_id)
        {
            Console.WriteLine($"Started listening for C2D messages at device '{device_id}'....");
            long totalBytesDownloaded = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                Message receivedMessage = null;
                try {
                    receivedMessage = await _deviceClient.ReceiveAsync(cancellationToken);//TimeSpan.FromSeconds(1));
                } catch (Exception x) {
                    Console.WriteLine("{0}: Exception hit when receiving the message, ignoring it: {1}", DateTime.Now, x.Message);
                    continue;
                }

                if (receivedMessage != null && !_isPaused)
                {
                    string messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());

                    // Read properties from the received message
                    int chunkIndex = int.Parse(receivedMessage.Properties["chunk_index"]);
                    int totalChunks = int.Parse(receivedMessage.Properties["total_chunks"]);

                    try {
                        JObject messageObject = JObject.Parse(messageData);

                        string filename = messageObject.Value<string>("filename");
                        // int chunkIndex = messageObject.Value<int>("chunk_index");
                        int writePosition = messageObject.Value<int>("write_position");
                        // int totalChunks = messageObject.Value<int>("total_chunks");
                        string uuencodedData = messageObject.Value<string>("data");

                        // byte[] bytes = StringToByteArray(data);
                        // string uuencodedData = messagePayload["data"].ToString();
                        byte[] bytes = Convert.FromBase64String(uuencodedData);
                        totalBytesDownloaded = await WriteChunkToFile(filename, writePosition, bytes, _stopwatch, totalBytesDownloaded, 100 * chunkIndex / totalChunks);

                        // Console.WriteLine("{0}: Received chunk {1} of {2} for file {3}", DateTime.Now, chunkIndex, totalChunks, filename);

                        await _deviceClient.CompleteAsync(receivedMessage); // Removes from the queue
                    } catch (Exception x) {
                        Console.WriteLine("{0}: Exception hit when parsing the message, ignoring it: {1}", DateTime.Now, x.Message);
                        continue;
                    }
                }
            }
        }

        private static async Task<long> WriteChunkToFile(string filename, int writePosition, byte[] bytes, Stopwatch stopwatch, long writtenAmount = -1, int progressPercent = 0)
        {
            if(writtenAmount < 0) writtenAmount = writePosition;
            long totalBytesDownloaded = writtenAmount + bytes.Length;
            string path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            using (FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) // Use FileShare.Write for shared access (worker threads?)
            {
                // fileStream.Position = chunkIndex * bytes.Length;
                fileStream.Seek(writePosition, SeekOrigin.Begin);
                // fileStream.Write(bytes, 0, bytes.Length);
                await fileStream.WriteAsync(bytes, 0, bytes.Length);
            }
            if(!stopwatch.IsRunning) {
                stopwatch.Start();
                totalBytesDownloaded = bytes.Length;
            }
            double timeElapsedInSeconds = stopwatch.Elapsed.TotalSeconds;
            double throughput = totalBytesDownloaded / timeElapsedInSeconds / 1024.0; // in KiB/s

            Console.WriteLine($"%{progressPercent:00} @pos: {writePosition:00000000000} tot: {writtenAmount:00000000000} Throughput: {throughput:0.00} KiB/s");
            return totalBytesDownloaded;
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
            Console.WriteLine($"Started listening for HTTP....");
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await _httpListener.GetContextAsync();
                HttpListenerRequest request = context.Request;

                if (request.HttpMethod == "GET")
                {
                    if (request.Url.AbsolutePath.ToLower() == "/busy")
                    {
                        _isPaused = true;
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/ready")
                    {
                        _isPaused = false;
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/update")
                    {
                        SendFirmwareUpdateReady(cancellationToken, GetDeviceIdFromConnectionString(_deviceConnectionString), "Microsoft Azure Storage Explorer.app.zip"); // Async call for update
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/continue")
                    {
                        string fromPos = request.QueryString["from"];
                        string filename = request.QueryString["file"];
                        SendFirmwareUpdateReadyContd(cancellationToken,
                                                     GetDeviceIdFromConnectionString(_deviceConnectionString),
                                                     string.IsNullOrEmpty(filename) ? "Microsoft Azure Storage Explorer.app.zip" : filename,
                                                     string.IsNullOrEmpty(fromPos) ? -1 : int.Parse(fromPos)); // Async call for update
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/shutdown")
                    {
                        _isPaused = true;
                        _isShutdown = true;
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
