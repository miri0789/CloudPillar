using System.Net;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text.RegularExpressions;

namespace FirmwareUpdateAgent
{
    partial class Program
    {
        private static DeviceClient _deviceClient;
        private static string _deviceConnectionString;
        private static Mutex _mutex;
        private static HttpListener _httpListener;
        private static Stopwatch _stopwatch = new Stopwatch();
        private static TwinAction? _downloadAction = null;

        /// <summary>
        /// The entry point of the FirmwareUpdateAgent application.
        /// Initializes the device client, sets up desired properties update callback, handles cancellation,
        /// sets up an HTTP listener for external commands, and starts the main loop for handling HTTP requests.
        /// </summary>
        /// <param name="args">Command-line arguments (not used in this application).</param>
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
                    _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, GetTransportType());
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

        /// <summary>
        /// Retrieves the transport type from the TRANSPORT_TYPE environment variable, or defaults to AMQP.
        /// </summary>
        /// <returns>The transport type.</returns>
        private static TransportType GetTransportType()
        {
            var transportTypeString = Environment.GetEnvironmentVariable("TRANSPORT_TYPE");
            return Enum.TryParse(transportTypeString, out TransportType transportType)
                ? transportType
                : TransportType.Amqp;
        }

        /// <summary>
        /// Sends a FirmwareUpdateReady event to continue the firmware update process.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the task.</param>
        /// <param name="c2dSubscription">C2D subscription instance.</param>
        /// <param name="device_id">Device ID.</param>
        /// <param name="filename">Filename of the firmware update file.</param>
        private static async Task SendFirmwareUpdateReadyContd(CancellationToken cancellationToken, C2DSubscription c2dSubscription, string device_id, string filename, string? rewind = null)
        {
            Console.WriteLine($"SDK command 'Continue' at device '{device_id}'....");
            string path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            FileInfo fi = new FileInfo(path);
            long startFromPos = fi.Exists ? fi.Length : 0L;
            if (!string.IsNullOrEmpty(rewind)) {
                startFromPos -= long.Parse(rewind);
                if(startFromPos < 0L) startFromPos = 0;
            }
            await SendFirmwareUpdateReady(cancellationToken, c2dSubscription, device_id, filename, startFromPos);
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

            TwinAction? action = _downloadAction;
            action?.ReportProgress(progressPercent);
            if (progressPercent == 100)
                action?.ReportSuccess("FinishedTransit", "Finished streaming as the last chunk arrived.");
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

        /// <summary>
        /// Handles the HTTP Listener for simulating commands from the SDK.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the task.</param>
        /// <param name="c2dSubscription">C2D subscription instance.</param>
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
                    else if (request.Url.AbsolutePath.ToLower() == "/twin")
                    {
                        _ = ExecTwinActions(cancellationToken, c2dSubscription, _deviceClient);
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/update")
                    {
                        string? fromPos = request.QueryString["from"];
                        string? filename = request.QueryString["file"];
                        _ = SendFirmwareUpdateReady(cancellationToken, c2dSubscription,
                                                     GetDeviceIdFromConnectionString(_deviceConnectionString),
                                                     string.IsNullOrEmpty(filename) ? "Microsoft Azure Storage Explorer.app.zip" : filename,
                                                     string.IsNullOrEmpty(fromPos) ? -1L : long.Parse(fromPos)); // Async call for update
                    }
                    else if (request.Url.AbsolutePath.ToLower() == "/continue")
                    {
                        string? fromPos = request.QueryString["from"];
                        string? filename = request.QueryString["file"];
                        if(string.IsNullOrEmpty(fromPos))
                            _ = SendFirmwareUpdateReadyContd(cancellationToken, c2dSubscription,
                                                     GetDeviceIdFromConnectionString(_deviceConnectionString),
                                                     string.IsNullOrEmpty(filename) ? "Microsoft Azure Storage Explorer.app.zip" : filename); // Async call for update
                        else
                            _ = SendFirmwareUpdateReady(cancellationToken, c2dSubscription,
                                                     GetDeviceIdFromConnectionString(_deviceConnectionString),
                                                     string.IsNullOrEmpty(filename) ? "Microsoft Azure Storage Explorer.app.zip" : filename,
                                                     long.Parse(fromPos)); // Async call for update
                    }
                }

                using HttpListenerResponse response = context.Response;
                string responseString = !c2dSubscription.IsSubscribed ? "Agent is paused" : "Agent is running";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
        }

        /// <summary>
        /// Gets the device ID from the connection string.
        /// </summary>
        /// <param name="connectionString">Device connection string.</param>
        /// <returns>Device ID.</returns>
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

        /// <summary>
        /// Executes the twin actions based on the device twin state, handles the firmware update process and reports the progress.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <param name="c2dSubscription">An object representing the C2D subscription for the device.</param>
        /// <param name="deviceClient">A device client instance to interact with IoT Hub.</param>
        /// <returns>List of actions performed.</returns>
        private static async Task<List<TwinAction>> ExecTwinActions(CancellationToken cancellationToken, C2DSubscription c2dSubscription, DeviceClient deviceClient)
        {
            /***
            This method is responsible for executing twin actions based on the device twin state. It checks 
            if the desired properties contain the "protocol" key, and if so, it handles the firmware update 
            process by calling the appropriate methods (SendFirmwareUpdateReady and SendFirmwareUpdateReadyContd)
            and reports the progress. 
            If the desired properties do not contain the "protocol" key, it reports the action as completed. 
            It also waits for the action to be completed and persists the action state.
            ***/

            // Report the current device twin state and perform actions based on the state
            return await TwinAction.ReportTwinState(cancellationToken, deviceClient,
                            c2dSubscription.IsSubscribed ? "READY" : "BUSY",
                            async (CancellationToken cancellationToken, TwinAction action) =>
                            {
                                try
                                {
                                    switch(action.Desired["action"]?.Value<string>())
                                    {
                                        case "singularDownload": {
                                            // Set the current action being executed
                                            _downloadAction = action;
                                            if (action.IsInProgress || /*DEMO ONLY TBD REMOVE*/ action.Desired.ContainsKey("retransmissionRewind"))
                                            {
                                                await SendFirmwareUpdateReadyContd(cancellationToken, c2dSubscription,
                                                                        GetDeviceIdFromConnectionString(_deviceConnectionString),
                                                                        action.Desired["source"]?.ToString(),
                                                                        action.Desired["retransmissionRewind"]?.ToString() );
                                            }
                                            else
                                            {
                                                // Report the initial progress and call the SendFirmwareUpdateReady method
                                                action.ReportProgress();
                                                await SendFirmwareUpdateReady(cancellationToken, c2dSubscription, 
                                                                        GetDeviceIdFromConnectionString(_deviceConnectionString), 
                                                                        action.Desired["source"]?.ToString());
                                            }
                                            break;
                                        }
                                        case "periodicUpload": 
                                        {
                                            string filename = action.Desired["filename"].Value<string>();
                                            int interval = action.Desired["interval"].Value<int>();
                                            bool enabled = action.Desired["enabled"].Value<bool>();

                                            if (enabled)
                                            {
                                                _ =  UploadFilePeriodicallyAsync(action, filename, TimeSpan.FromSeconds(interval));
                                            }
                                            break;
                                        }
                                        default:
                                        {
                                            action.ReportSuccess("0", "Operation Completed Successfully"); // Currently not implemented, TBD
                                            break;
                                        }
                                    }
                                    // Wait for the action to be completed
                                    while (!action.IsComplete)
                                    {
                                        // Adjust the delay time as needed (currently 5 seconds)
                                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                                        // Persist the action state
                                        await action.Persist();
                                    }
                                }
                                finally
                                {
                                    // Reset the current action to null
                                    _downloadAction = null;
                                }
                            });
        }
        private static async Task UploadFilePeriodicallyAsync(TwinAction action, string filename, TimeSpan interval)
        {
            while (true)
            {
                try
                {
                    await UploadFileToBlobStorageAsync(action, filename);
                    action.ReportSuccess("Done", "Will keep uploading " + interval.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading file '{filename}': {ex.Message}");
                    action.ReportFailed(ex.GetType().Name, ex.Message);
                }

                await Task.Delay(interval);
            }
        }

        private static async Task UploadFileToBlobStorageAsync(TwinAction action, string fullFilePath)
        {
            string filename = Regex.Replace(fullFilePath.Replace("//:", "_protocol_").Replace("\\", "/").Replace(":/", "_driveroot_/"), "^\\/", "_root_");//Path.GetFileName(fullFilePath);

            var sasUriResponse = await _deviceClient.GetFileUploadSasUriAsync(new FileUploadSasUriRequest
            {
                BlobName = filename
            });

            var storageUri = sasUriResponse.GetBlobUri();
            var blob = new CloudBlockBlob(storageUri);
            await blob.UploadFromFileAsync(fullFilePath);

            await _deviceClient.CompleteFileUploadAsync(new FileUploadCompletionNotification
            {
                CorrelationId = sasUriResponse.CorrelationId,
                IsSuccess = true
            });
        }
    }
}
