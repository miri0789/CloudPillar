using System.Net;
using System.Text;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Microsoft.Azure.Devices.Shared;

namespace FirmwareUpdateAgent
{
    class Program
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

        public class TwinAction
        {
            private const string _ChangeSpecKey = "changeSpec";
            private readonly string _changeSpec, _recipe, _stage, _path;
            private readonly int _step;
            // private readonly Twin _twin;
            private readonly DeviceClient _deviceClient;
            private readonly JObject _desired;
            private readonly JObject _reported;

            public TwinAction(DeviceClient deviceClient, JObject desired, JObject reported, string changeSpec, string recipe, string stage, int step, string path)
            {
                _deviceClient = deviceClient;
                // _twin = twin;
                _desired = desired;
                _reported = reported;
                _changeSpec = changeSpec;
                _recipe = recipe;
                _stage = stage;
                _step = step;
                _path = path;
            }

            public JObject Desired { get { return (_desired[_changeSpec]![_recipe]![_stage]![_step]! as JObject)!; } }
            public JObject? Reported { 
                get { 
                    try {
                        return _reported[_changeSpec]?[_recipe]?[_stage]?[_step] as JObject; 
                    } catch(Exception x) {
                        return null;
                    }
                } 
            }
            private void EnsureReported()
            {
                if (!_reported.ContainsKey(_changeSpec))
                {
                    _reported[_changeSpec] = JObject.Parse("{}");
                }
                if (!(_reported[_changeSpec]! as JObject)!.ContainsKey(_recipe))
                {
                    _reported[_changeSpec]![_recipe] = JObject.Parse("{}");
                }
                if (!(_reported[_changeSpec]![_recipe] as JObject)!.ContainsKey(_stage))
                {
                    _reported[_changeSpec]![_recipe]![_stage] = JContainer.Parse("[]");
                }

                JContainer stageObject = (_reported[_changeSpec]![_recipe]![_stage] as JContainer)!;
                for (int i = stageObject.Count; i < _step + 1; i++)
                    stageObject!.Add(JObject.Parse("{}"));
            }

            public void ReportPending() { Status = "Pending"; }
            public void ReportProgress(int percent = 0) { Status = "InProgress"; Reported!["Progress"] = percent; }
            public void ReportFailed(string ResultCode, string ResultText) { Status = "Failed"; Reported!["ResultCode"] = ResultCode; Reported!["ResultText"] = ResultText; }
            public void ReportComplete(string ResultCode, string ResultText) { Status = "Complete"; Reported!["ResultCode"] = ResultCode; Reported!["ResultText"] = ResultText; Reported!["Progress"]?.Remove(); }

            public String? Status
            {
                get { return Reported?["status"]?.ToString(); }
                set { EnsureReported(); Reported!["status"] = value; }
            }

            public bool IsPending { get { return "Pending" == Status; } }
            public bool IsInProgress { get { return "InProgress" == Status; } }
            public bool IsFailed { get { return "Failed" == Status; } }
            public bool IsComplete { get { return "Complete" == Status; } }

            public async Task Persist() {
                await UpdateReportedPropertiesAsync(_deviceClient, _ChangeSpecKey, _reported[_ChangeSpecKey] as JObject);
            }
            private static async Task UpdateReportedPropertiesAsync(DeviceClient deviceClient, string key, object value)
            {
                var updatedReportedProperties = new TwinCollection();
                updatedReportedProperties[key] = value;
                await deviceClient.UpdateReportedPropertiesAsync(updatedReportedProperties);
            }

            public static async Task<List<TwinAction>> ReportTwinState(CancellationToken cancellationToken, DeviceClient deviceClient, string deviceState, Func<CancellationToken, TwinAction, Task> processor = null)
            {   
                var actions = new List<TwinAction>();
                try {
                    var currentTwin = await deviceClient.GetTwinAsync();

                    var desiredJObject = JObject.Parse(currentTwin.Properties.Desired.ToJson());
                    var reportedJObject = JObject.Parse(currentTwin.Properties.Reported.ToJson());

                    reportedJObject["deviceState"] = deviceState;
                    await UpdateReportedPropertiesAsync(deviceClient, "deviceState", deviceState);

                    JObject changeSpecJObject = (JObject)desiredJObject[_ChangeSpecKey]!;

                    if (!JObject.DeepEquals(changeSpecJObject["id"], reportedJObject[_ChangeSpecKey]?["id"]))
                    {
                        reportedJObject[_ChangeSpecKey] = JObject.Parse("{\"id\": \"" + changeSpecJObject["id"] + "\"}");
                    }

                    foreach (var recipe in changeSpecJObject)
                    {
                        JObject? recipeJObject = recipe.Value as JObject;
                        if (recipeJObject == null) continue;
                        var nextRecipe = recipeJObject.Parent!.Next;
                        bool goingFallback = false;
                        foreach (var stage in recipeJObject)
                        {
                            if(goingFallback) {
                                break; // Go to next recipe
                            }
                            JContainer? stageJContainer = stage.Value as JContainer;
                            if (stageJContainer == null) continue;
                            for (int i = 0; i < stageJContainer.Count; i++)
                            {
                                var step = stageJContainer[i]!;
                                var path = step.Path;
                                var action = new TwinAction(deviceClient, desiredJObject, reportedJObject, _ChangeSpecKey, recipe.Key, stage.Key, i, path);
                                if(action.Status == null || action.IsPending || action.IsInProgress)
                                    actions.Add(action);
                                else
                                if(action.IsFailed) {
                                    reportedJObject[_ChangeSpecKey]!["Status"] = nextRecipe != null ? $"Falling back to next recipe '{(nextRecipe as JProperty).Name}'" : "Failed";
                                    reportedJObject[_ChangeSpecKey]!["lastFaultedRecipe"] = recipe.Key;
                                    reportedJObject[_ChangeSpecKey]!["lastFaultedPath"] = path;
                                    await action.Persist();//UpdateReportedPropertiesAsync(deviceClient, _ChangeSpecKey, reportedJObject[_ChangeSpecKey] as JObject);
                                    if(nextRecipe == null) // Fatal error, no fallback
                                        return actions;
                                    else
                                        goingFallback = true;
                                }
                            }
                        }
                        // Finished 1 recipe, checking results
                        if(goingFallback && nextRecipe != null) {
                            // Last recipe failed, clean actions try next one
                            actions.Clear();
                        } else {
                            if (actions.Count == 0) { // Empty list and no fallback means Success
                                reportedJObject[_ChangeSpecKey]!["Status"] = "Complete";
                            }
                            break;
                        }
                    }
                    foreach(var action in actions) {
                        if(cancellationToken.IsCancellationRequested)
                            break;
                        if(!action.IsInProgress) // Leave InProgress intact
                            action.ReportPending();
                    }
                    await UpdateReportedPropertiesAsync(deviceClient, _ChangeSpecKey, reportedJObject[_ChangeSpecKey] as JObject);

                    if (processor != null) {
                        foreach(var action in actions) {
                            if(cancellationToken.IsCancellationRequested)
                                break;
                            reportedJObject[_ChangeSpecKey]!["actionPath"] = action._path;
                            reportedJObject[_ChangeSpecKey]!["Status"] = "InProgress";
                            await action.Persist();
                            try {
                                await processor(cancellationToken, action);
                            } catch (Exception x) {
                                action.ReportFailed(x.GetType().Name, x.Message);
                            }
                            await action.Persist();
                            if(action.IsFailed) 
                                break;
                        }
                    }
                } catch (Exception x) {
                    Console.WriteLine("{0}: Exception hit when analysing the twin: {1}", DateTime.Now, x.Message);
                }
                return actions;
            }
        };

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
