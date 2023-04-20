using System.Net;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Diagnostics;

namespace FirmwareUpdateAgent
{
    partial class Program
    {
        private static DeviceClient _deviceClient;
        private static string _deviceConnectionString;
        private static Mutex _mutex;
        private static HttpListener _httpListener;
        private static Stopwatch _stopwatch = new Stopwatch();
        private static bool _isShutdown = false;
        private static TwinAction? _currentAction = null;

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
                    Console.WriteLine("Starting Simulated device...");

                    var cts = new CancellationTokenSource();
                    _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, TransportType.Amqp);
                    var c2dSubscription = new C2DSubscription(_deviceClient, GetDeviceIdFromConnectionString(_deviceConnectionString));

                    await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                            async (desiredProperties, userContext) =>
                            {
                                Console.WriteLine("Desired properties were updated:");
                                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));
                                await ExecTwinActions(cts.Token, c2dSubscription, _deviceClient);
                            }, null);

                    Console.CancelKeyPress += (sender, eventArgs) =>
                    {
                        Console.WriteLine("Cancelling...");
                        cts.Cancel();
                        eventArgs.Cancel = true;
                    };

                    _httpListener = new HttpListener();
                    _httpListener.Prefixes.Add("http://+:8099/");
                    _httpListener.Start();

                    // Add the initial Subscribe() call here
                    await c2dSubscription.Subscribe(cts.Token);

                    var httpTask = Task.Run(() => HandleHttpListener(cts.Token, c2dSubscription));


                    // Wait for any of the tasks to complete or be cancelled
                    await httpTask;

                    Console.WriteLine("Exiting...");
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
                        try
                        {
                            mutex?.ReleaseMutex();
                        }
                        catch (Exception) { }
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

        private static async Task SendFirmwareUpdateReadyContd(CancellationToken cancellationToken, C2DSubscription c2dSubscription, string device_id, string filename, long startFromPos = -1)
        {
            Console.WriteLine($"SDK command 'Continue' at device '{device_id}'....");
            string path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            FileInfo fi = new FileInfo(path);
            await SendFirmwareUpdateReady(cancellationToken, c2dSubscription, device_id, filename, startFromPos >= 0 ? startFromPos : fi.Exists ? fi.Length : 0L);
        }
        private static async Task SendFirmwareUpdateReady(CancellationToken cancellationToken, C2DSubscription c2dSubscription, string device_id, string filename, long startFromPos = -1L)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (c2dSubscription.IsSubscribed)
                {
                    Console.WriteLine($"Sending FirmwareUpdateReady event at device '{device_id}'....");

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
                }
                else
                {
                    Console.WriteLine($"Can't send FirmwareUpdateReady event while paused at device '{device_id}'....");
                    _stopwatch.Reset(); // Resetting stopwatch causes next throughput calculations to reset
                }

                await Task.Delay(TimeSpan.FromSeconds(60), cancellationToken);
            }
        }

        private static async Task<long> WriteChunkToFile(string filename, int writePosition, byte[] bytes, Stopwatch stopwatch, long writtenAmount = -1, int progressPercent = 0)
        {
            if (writtenAmount < 0) writtenAmount = writePosition;
            long totalBytesDownloaded = writtenAmount + bytes.Length;
            string path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            using (FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None)) // Use FileShare.Write for shared access (worker threads?)
            {
                // fileStream.Position = chunkIndex * bytes.Length;
                fileStream.Seek(writePosition, SeekOrigin.Begin);
                // fileStream.Write(bytes, 0, bytes.Length);
                await fileStream.WriteAsync(bytes, 0, bytes.Length);
            }
            if (!stopwatch.IsRunning)
            {
                stopwatch.Start();
                totalBytesDownloaded = bytes.Length;
            }
            double timeElapsedInSeconds = stopwatch.Elapsed.TotalSeconds;
            double throughput = totalBytesDownloaded / timeElapsedInSeconds / 1024.0; // in KiB/s

            TwinAction? action = _currentAction;
            action?.ReportProgress(progressPercent);
            if(progressPercent == 100) 
                action?.ReportComplete("FinishedTransit", "Finished streaming as the last chunk arrived.");
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

        private static async Task HandleHttpListener(CancellationToken cancellationToken, C2DSubscription c2dSubscription)
        {
            Console.WriteLine($"Started listening for HTTP....");
            _ = ExecTwinActions(cancellationToken, c2dSubscription, _deviceClient); // First time just hook up to the twin status
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext context = await _httpListener.GetContextAsync();
                HttpListenerRequest request = context.Request;

                if (request.HttpMethod == "GET")
                {
                    if (request.Url.AbsolutePath.ToLower() == "/busy")
                    {
                        Console.WriteLine("Pausing Agent");
                        c2dSubscription.Unsubscribe();
                        _ = ExecTwinActions(cancellationToken, c2dSubscription, _deviceClient);
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/ready")
                    {
                        Console.WriteLine("Resuming Agent");
                        await c2dSubscription.Subscribe(cancellationToken);
                        _ = ExecTwinActions(cancellationToken, c2dSubscription, _deviceClient);
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/update")
                    {
                        _ = SendFirmwareUpdateReady(cancellationToken, c2dSubscription, GetDeviceIdFromConnectionString(_deviceConnectionString), "Microsoft Azure Storage Explorer.app.zip"); // Async call for update
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/twin")
                    {
                        _ = ExecTwinActions(cancellationToken, c2dSubscription, _deviceClient);
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/continue")
                    {
                        string fromPos = request.QueryString["from"];
                        string filename = request.QueryString["file"];
                        _ = SendFirmwareUpdateReadyContd(cancellationToken, c2dSubscription,
                                                     GetDeviceIdFromConnectionString(_deviceConnectionString),
                                                     string.IsNullOrEmpty(filename) ? "Microsoft Azure Storage Explorer.app.zip" : filename,
                                                     string.IsNullOrEmpty(fromPos) ? -1L : long.Parse(fromPos)); // Async call for update
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/shutdown")
                    {
                        c2dSubscription.Unsubscribe();
                        _isShutdown = true;
                    }
                }

                using HttpListenerResponse response = context.Response;
                string responseString = !c2dSubscription.IsSubscribed ? "Agent is paused" : "Agent is running";
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

        private static async Task ExecTwinActions(CancellationToken cancellationToken, C2DSubscription c2dSubscription, DeviceClient deviceClient)
        {
            var actions = await TwinAction.ReportTwinState(cancellationToken, deviceClient, 
                            c2dSubscription.IsSubscribed ? "READY" : "BUSY", 
                            async (CancellationToken cancellationToken, TwinAction action) => {
                _currentAction = action;
                try {
                    if(action.Desired.ContainsKey("protocol")) {
                        if(action.IsInProgress) {
                            await SendFirmwareUpdateReadyContd(cancellationToken, c2dSubscription,
                                                        GetDeviceIdFromConnectionString(_deviceConnectionString),
                                                        action.Desired["source"]?.ToString());
                        } else {
                            action.ReportProgress();
                            await SendFirmwareUpdateReady(cancellationToken, c2dSubscription, GetDeviceIdFromConnectionString(_deviceConnectionString), action.Desired["source"]?.ToString()); 
                        }
                    } else {
                        // Do nothing for a while
                        action.ReportComplete("0", "Operation Completed Successfully");
                    }
                    // Wait for the action to be completed
                    while (!action.IsComplete)
                    {
                        // You can adjust the delay time as needed
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                        await action.Persist();
                    }  
                } finally {
                    _currentAction = null;
                }                  
            });
        }
    }
}
