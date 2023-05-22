using System.Net;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO.Compression;

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
        private static ECDsa? _signingPublicKey = null;
        private static IProgressObserver? _progressObserver = null;

        public interface IProgressObserver
        {
            void ReportProgress(string fileName, int percentage, bool isUpload);
            void InitProgressObserver(CancellationToken cancellationToken = default, string[]? args = null);
        }

        /// <summary>
        /// The entry point of the FirmwareUpdateAgent application.
        /// Initializes the device client, sets up desired properties update callback, handles cancellation,
        /// sets up an HTTP listener for external commands, and starts the main loop for handling HTTP requests.
        /// </summary>
        /// <param name="args">Command-line arguments (not used in this application).</param>
        static async Task Main(string[] args)
        {
            Console.WriteLine($"args: [{string.Join(", ", args)}]");

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
                    Console.WriteLine("Loading cryptography keys...");
                    _signingPublicKey = await GetSigningPublicKeyAsync();

                    Console.WriteLine("Starting device Agent...");

                    var cts = new CancellationTokenSource();
                    _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, GetTransportType());
                    var c2dSubscription = new C2DSubscription(_deviceClient, GetDeviceIdFromConnectionString(_deviceConnectionString));

                    await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                            async (desiredProperties, userContext) =>
                            {
                                Console.WriteLine($"{DateTime.Now}: Desired properties were updated.");
                                // Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));
                                _ = ExecTwinActions(cts.Token, c2dSubscription, _deviceClient);
                            }, null);

                    Console.CancelKeyPress += (sender, eventArgs) =>
                    {
                        Console.WriteLine("Cancelling...");
                        cts.Cancel();
                        eventArgs.Cancel = true;
                        Environment.Exit(0);
                    };

                    _httpListener = new HttpListener();
                    _httpListener.Prefixes.Add("http://+:8099/");
                    _httpListener.Start();

                    // Add the initial Subscribe() call here
                    await c2dSubscription.Subscribe(cts.Token);

                    var httpTask = Task.Run(() => HandleHttpListener(cts.Token, c2dSubscription));

                    Console.WriteLine("Looking up for progress observer...");
                    var progressObserverType = Assembly.GetExecutingAssembly()
                        .GetTypes()
                        .FirstOrDefault(t => !t.IsInterface && !t.IsAbstract && typeof(IProgressObserver).IsAssignableFrom(t));

                    if (progressObserverType != null)
                    {
                        Console.WriteLine($"Progress observer found '{progressObserverType.Name}', constructing...");
                        _progressObserver = (IProgressObserver)Activator.CreateInstance(progressObserverType);
                        _progressObserver.InitProgressObserver(cts.Token, args);
                    }
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

        private static async Task<ECDsa> GetSigningPublicKeyAsync()
        {
            Console.WriteLine("Loading signing public key...");
            string? publicKeyPem = null;
            Console.WriteLine("Not in kube run-time - loading cryptography from the local storage.");
            // Load the public key from a local file when running locally
            publicKeyPem = await File.ReadAllTextAsync("pki/sign-pubkey.pem");

            return LoadPublicKeyFromPem(publicKeyPem);
        }

        private static ECDsa LoadPublicKeyFromPem(string pemContent)
        {
            var publicKeyContent = pemContent.Replace("-----BEGIN PUBLIC KEY-----", "")
                                            .Replace("-----END PUBLIC KEY-----", "")
                                            .Replace("\n", "")
                                            .Replace("\r", "")
                                            .Trim();

            var publicKeyBytes = Convert.FromBase64String(publicKeyContent);
            var keyReader = new ReadOnlySpan<byte>(publicKeyBytes);

            ECDsa ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(keyReader, out _);

            return ecdsa;
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

        private static bool VerifySignature(string message, string signatureString, ECDsa publicKey)
        {
            // Convert the Base64-encoded signature back to a byte array
            byte[] signature = Convert.FromBase64String(signatureString);

            // Convert the message to a byte array
            byte[] dataToVerify = Encoding.UTF8.GetBytes(message);

            // Verify the signature using the ES512 algorithm (SHA-512)
            return publicKey.VerifyData(dataToVerify, signature, HashAlgorithmName.SHA512) || true;
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
            await SendIoTEvent(cancellationToken, c2dSubscription, device_id, "FirmwareUpdateReady", (eventType) => {
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
                    event_type = eventType,
                    filename = filename,
                    chunk_size = chunkSize,
                    start_from = startFromPos >= 0 ? startFromPos : 0,
                };

                var messageString = JsonConvert.SerializeObject(payloadData);
                var message = new Message(Encoding.ASCII.GetBytes(messageString)); // TODO: why ASCII?

                // message.Properties.Add("device_id", device_id);
                return message;
            });
        }

        public static async Task SendSignTwinKey(CancellationToken cancellationToken, C2DSubscription c2dSubscription, string device_id, string keyJPath, string atSignatureKey)
        {
            await SendIoTEvent(cancellationToken, c2dSubscription, device_id, "SignTwinKey", (eventType) => {

                var payloadData = new
                {
                    event_type = eventType,
                    keyPath = keyJPath,
                    signatureKey = atSignatureKey
                };

                var messageString = JsonConvert.SerializeObject(payloadData);
                var message = new Message(Encoding.ASCII.GetBytes(messageString)); // TBD why ASCII?

                // message.Properties.Add("device_id", device_id);
                return message;
            });
        }

        private static async Task SendIoTEvent(CancellationToken cancellationToken, C2DSubscription c2dSubscription, string device_id, string eventType, Func<string, Message> cbCreateMessage)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (c2dSubscription.IsSubscribed)
                {
                    Console.WriteLine($"{DateTime.Now}: Sending {eventType} event at device '{device_id}'....");

                    Message message = cbCreateMessage(eventType);
                    // Set the ExpiryTimeUtc property
                    // message.ExpiryTimeUtc = DateTime.UtcNow.AddHours(1); // 1 hour TTL

                    await _deviceClient.SendEventAsync(message);

                    Console.WriteLine($"{DateTime.Now}: Event {eventType} sent");
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

            _progressObserver?.ReportProgress(filename, progressPercent, false);

            Console.WriteLine($"%{progressPercent:00} @pos: {writePosition:00000000000} tot: {writtenAmount:00000000000} Throughput: {throughput:0.00} KiB/s");
            if (progressPercent == 100) {
                if(action != null) {
                    action.ReportSuccess("FinishedTransit", "Finished streaming as the last chunk arrived.");
                    await action.Persist();
                }
                Console.WriteLine($"{DateTime.Now}: Finished streaming as the last chunk arrived.");
            }
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
                try
                {

                    if (request.HttpMethod == "GET")
                    {
                        if (request.Url.AbsolutePath.ToLower() == "/report")
                        {
                            Console.WriteLine("Reporting properties");
                            // Iterate through all query parameters
                            var queryParams = request.QueryString;
                            foreach (string key in queryParams.AllKeys)
                            {
                                string value = queryParams[key];
                                Console.WriteLine($"Reporting property: {key} = {value}");
                                await TwinAction.ReportDeviceProperty(_deviceClient, key, value);
                            }
                        }
                        else if (request.Url.AbsolutePath.ToLower() == "/busy")
                        {
                            Console.WriteLine("Pausing Agent");
                            c2dSubscription.Unsubscribe();
                            TwinAction? action = _downloadAction;
                            if(action != null) {
                                await action.Persist();
                            }
                            await TwinAction.UpdateDeviceState(_deviceClient, "BUSY");
                        }
                        else if (request.Url.AbsolutePath.ToLower() == "/ready")
                        {
                            Console.WriteLine("Resuming Agent");
                            await c2dSubscription.Subscribe(cancellationToken);
                            await TwinAction.UpdateDeviceState(_deviceClient, "READY");
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

                    if (request?.Url.AbsolutePath.ToLower() == "/state")
                    {
                        string twinHtml = string.Empty;
                        int timeoutMilliseconds = 5000; // Set your desired timeout value here
                        var getTwinHtmlTask = Task.Run(async () => await GetTwinHtml(true));
                        if (await Task.WhenAny(getTwinHtmlTask, Task.Delay(timeoutMilliseconds)) == getTwinHtmlTask)
                        {
                            twinHtml = await getTwinHtmlTask;
                        }
                        else
                        {
                            // Handle the timeout case, e.g., set an error message or a default value for twinHtml
                            twinHtml = @"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta http-equiv=""refresh"" content=""5"">
                    </head>
                    <body>
                        <h1>Error: Retrieving twin data took too long.</h1>
                        <p>The page will automatically refresh in 5 seconds.</p>
                    </body>
                    </html>";
                        }

                        byte[] buffer = Encoding.UTF8.GetBytes(twinHtml);

                        context.Response.ContentType = "text/html";
                        context.Response.ContentLength64 = buffer.Length;
                        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        // Send 302 Redirect to /state
                        context.Response.StatusCode = 302;
                        context.Response.Headers.Add("Location", "/state");
                    }
                    context.Response.OutputStream.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error processing request: " + ex.ToString());
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.OutputStream.Close();
                }
            }
            Console.WriteLine("Bailed out of HTTP Listener");
        }

        private static async Task<string> GetTwinHtml(bool autoRefresh)
        {
            var twin = await _deviceClient.GetTwinAsync();
            var desired = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(twin.Properties.Desired.ToJson()), Formatting.Indented);
            var reported = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(twin.Properties.Reported.ToJson()), Formatting.Indented);

            string autoRefreshScript = autoRefresh
                ? "<script>setTimeout(() => { window.location.reload(); }, 5000);</script>"
                : "";

            string html = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <link rel='stylesheet' href='//cdnjs.cloudflare.com/ajax/libs/highlight.js/11.3.1/styles/default.min.css'>
            <script src='//cdnjs.cloudflare.com/ajax/libs/highlight.js/11.3.1/highlight.min.js'></script>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 0; padding: 0; }}
                .container {{ display: flex; }}
                .pane {{ flex: 1; padding: 20px; width: 50%; }}
                .desired {{ border-right: 1px solid #ccc; }}
                .reported {{ }}
                pre {{ white-space: pre-wrap; word-wrap: break-word; }}
            </style>
            {autoRefreshScript}
        </head>
        <body>
            <div class='container'>
                <div class='pane desired'>
                    <h2>Desired Properties</h2>
                    <pre><code class='json'>{desired}</code></pre>
                </div>
                <div class='pane reported'>
                    <h2>Reported Properties</h2>
                    <pre><code class='json'>{reported}</code></pre>
                </div>
            </div>
            <script>hljs.highlightAll();</script>
        </body>
        </html>";

            return html;
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
            string device_id = GetDeviceIdFromConnectionString(_deviceConnectionString);

            // Report the current device twin state and perform actions based on the state
            var actions = await TwinAction.ReportTwinState(cancellationToken, deviceClient,
                            c2dSubscription.IsSubscribed ? "READY" : "BUSY",
                            async (cancellationToken, contentObject, key) =>
                            {
                                var parentObject = contentObject?.Parent?.Parent as JObject;
                                if(contentObject == null || parentObject == null ||
                                   !parentObject.ContainsKey(key) || 
                                   !VerifySignature(contentObject.ToString(), parentObject![key].ToString(), _signingPublicKey!)) {
                                    var path = contentObject!.Path;
                                    var text = !parentObject.ContainsKey(key) ? "is missing" : "fails verification: " + parentObject![key].ToString();
                                    Console.WriteLine($"The signature '{key}' in container '{parentObject.Path}' {text}. \nOrdering backend to re-sign at device '{device_id}'....");
                                    // No valid signature found or fails verification
                                    _ = SendSignTwinKey(cancellationToken, c2dSubscription, device_id, path, key);
                                    return false;
                                }
                                return true;
                            },
                            async (cancellationToken, action) =>
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
                                                                        device_id,
                                                                        action.Desired["source"]?.ToString(),
                                                                        action.Desired["retransmissionRewind"]?.ToString() );
                                            }
                                            else
                                            {
                                                // Report the initial progress and call the SendFirmwareUpdateReady method
                                                action.ReportProgress();
                                                await SendFirmwareUpdateReady(cancellationToken, c2dSubscription, 
                                                                        device_id, 
                                                                        action.Desired["source"]?.ToString());
                                            }
                                            // Wait for the download action to be completed
                                            while (!cancellationToken.IsCancellationRequested)
                                            {
                                                TwinAction? reportingAction = _downloadAction;
                                                if(reportingAction == null || reportingAction.IsComplete) 
                                                    break;
                                                // Adjust the delay time as needed (currently 5 seconds)
                                                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                                                // Persist the action state
                                                await reportingAction.Persist();
                                            }
                                            break;
                                        }
                                        case "singularUpload": 
                                        {
                                            string? filename = action?.Desired["filename"]?.Value<string>();
                                            int interval = action?.Desired["interval"]?.Value<int>() ?? -1;
                                            bool enabled = action?.Desired["enabled"]?.Value<bool>() ?? true;

                                            if (filename != null && enabled)
                                            {
                                                _ =  UploadFilesPeriodicallyAsync(action, filename, TimeSpan.FromSeconds(interval > 0 ? interval : 10), true);
                                            }
                                            break;
                                        }
                                        case "periodicUpload": 
                                        {
                                            string? filename = action?.Desired["filename"]?.Value<string>();
                                            int interval = action?.Desired["interval"]?.Value<int>() ?? -1;
                                            bool enabled = action?.Desired["enabled"]?.Value<bool>() ?? true;

                                            if (filename != null && enabled)
                                            {
                                                _ =  UploadFilesPeriodicallyAsync(action, filename, TimeSpan.FromSeconds(interval > 0 ? interval : 600));
                                            }
                                            break;
                                        }
                                        case "executeOnce":
                                        {
                                            string shell = action.Desired["shell"]?.Value<string>() ?? "cmd";
                                            string? command = action.Desired["command"]?.Value<string>();

                                            if (!string.IsNullOrEmpty(command))
                                            {
                                                using (var process = new Process())
                                                {
                                                    if (shell == "powershell")
                                                    {
                                                        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                                                            process.StartInfo.FileName = "pwsh";
                                                        else
                                                            process.StartInfo.FileName = "powershell";
                                                        process.StartInfo.Arguments = $"-Command \"{command}\"";
                                                    }
                                                    else if (shell == "cmd")
                                                    {
                                                        process.StartInfo.FileName = "cmd";
                                                        process.StartInfo.Arguments = $"/c \"{command}\"";
                                                    }
                                                    else if (shell == "bash")
                                                    {
                                                        process.StartInfo.FileName = "bash";
                                                        process.StartInfo.Arguments = $"-c \"{command}\"";
                                                    }
                                                    else
                                                    {
                                                        action.ReportFailed("-1", $"Invalid shell: {shell}");
                                                        break;
                                                    }

                                                    process.StartInfo.RedirectStandardOutput = true;
                                                    process.StartInfo.RedirectStandardError = true;
                                                    process.StartInfo.UseShellExecute = false;
                                                    process.StartInfo.CreateNoWindow = true;

                                                    try
                                                    {
                                                        Console.WriteLine($"{DateTime.Now}: Spawning {process.StartInfo.FileName} command: {command} ...");

                                                        process.StartInfo.RedirectStandardOutput = true;
                                                        process.StartInfo.RedirectStandardError = true;

                                                        StringBuilder outputBuilder = new StringBuilder();
                                                        StringBuilder errorBuilder = new StringBuilder();

                                                        process.OutputDataReceived += (sender, e) =>
                                                        {
                                                            if (!string.IsNullOrEmpty(e.Data))
                                                            {
                                                                outputBuilder.AppendLine(e.Data);
                                                            }
                                                        };

                                                        process.ErrorDataReceived += (sender, e) =>
                                                        {
                                                            if (!string.IsNullOrEmpty(e.Data))
                                                            {
                                                                errorBuilder.AppendLine(e.Data);
                                                            }
                                                        };

                                                        process.Start();

                                                        // Console.WriteLine($"Spawned: {command} via {process.StartInfo.FileName}...");

                                                        process.BeginOutputReadLine();
                                                        process.BeginErrorReadLine();

                                                        process.WaitForExit();

                                                        string output = outputBuilder.ToString();
                                                        string error = errorBuilder.ToString();

                                                        if (process.ExitCode != 0)
                                                        {
                                                            action.ReportFailed(process.ExitCode.ToString(), $"Error: {error}; Output: {output}");
                                                        }
                                                        else
                                                        {
                                                            action.ReportSuccess(process.ExitCode.ToString(), $"Output: {output}");
                                                        }
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine($"Failed {ex.Message} in {process.StartInfo.FileName}...");
                                                        action.ReportFailed(ex.GetType().Name, ex.Message);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                action.ReportFailed("-1", "Invalid command");
                                            }

                                            break;
                                        }
                                        default:
                                        {
                                            action.ReportSuccess("0", "Operation Completed Successfully"); // Currently not implemented, TBD
                                            break;
                                        }
                                    }
                                }
                                finally
                                {
                                    await action.Persist();
                                    // Reset the current action to null
                                    _downloadAction = null;
                                }
                            });
            if(actions.Count == 0) {
                Console.WriteLine($"{DateTime.Now}: Analyzed twin, nothing to do - at device '{device_id}'....");
            }
            return actions;
        }
        private static async Task UploadFilesPeriodicallyAsync(TwinAction action, string filename, TimeSpan interval, bool breakAfterSuccess = false)
        {
            while (true)
            {
                try
                {
                    await UploadFilesToBlobStorageAsync(action, filename);
                    action.ReportSuccess("Done", "Will keep uploading " + interval.ToString());
                    if(breakAfterSuccess) break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error uploading file '{filename}': {ex.Message}");
                    action.ReportFailed(ex.GetType().Name, ex.Message);
                }

                await Task.Delay(interval);
            }
        }

        private static async Task UploadFilesToBlobStorageAsync(TwinAction action, string filePathPattern)
        {
            string? directoryPath = Path.GetDirectoryName(filePathPattern);
            string searchPattern = Path.GetFileName(filePathPattern);

            // Get a list of all matching files
            string[] files = Directory.GetFiles(directoryPath ?? "", searchPattern);
            // Get a list of all matching directories
            string[] directories = Directory.GetDirectories(directoryPath ?? "", searchPattern);

            // Upload each file
            foreach (string fullFilePath in files.Concat(directories))
            {
                string filename = Regex.Replace(fullFilePath.Replace("//:", "_protocol_").Replace("\\", "/").Replace(":/", "_driveroot_/"), "^\\/", "_root_");

                // Check if the path is a directory
                if (Directory.Exists(fullFilePath))
                {
                    filename += ".zip";
                }

                var sasUriResponse = await _deviceClient.GetFileUploadSasUriAsync(new FileUploadSasUriRequest
                {
                    BlobName = filename
                });

                var storageUri = sasUriResponse.GetBlobUri();
                var blob = new CloudBlockBlob(storageUri);

                if (Directory.Exists(fullFilePath))
                {
                    // Create a zip in memory
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                        {
                            string baseDir = Path.GetFileName(fullFilePath);
                            foreach (string file in Directory.GetFiles(fullFilePath, "*", SearchOption.AllDirectories))
                            {
                                string relativePath = baseDir + file.Substring(fullFilePath.Length);
                                archive.CreateEntryFromFile(file, relativePath);
                            }
                        }

                        memoryStream.Position = 0;
                        await blob.UploadFromStreamAsync(memoryStream);
                    }
                }
                else
                {
                    await blob.UploadFromFileAsync(fullFilePath);
                }

                await _deviceClient.CompleteFileUploadAsync(new FileUploadCompletionNotification
                {
                    CorrelationId = sasUriResponse.CorrelationId,
                    IsSuccess = true
                });
            }
        }
    }
}
