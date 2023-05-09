using System.Text;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using k8s;
using Microsoft.Azure.Devices.Shared;

namespace FirmwareUpdate
{
    // Main class for the Firmware Update Backend
    class Program
    {
        static string IotHubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING")!;
        static string StorageConnectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING")!;
        static string BlobContainerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME")!;
        static string EventHubCompatibleEndpoint = Environment.GetEnvironmentVariable("IOTHUB_EVENT_HUB_COMPATIBLE_ENDPOINT")!;
        static string EventHubCompatiblePath = Environment.GetEnvironmentVariable("IOTHUB_EVENT_HUB_COMPATIBLE_PATH")!;
        static string? PartitionId = Environment.GetEnvironmentVariable("PARTITION_ID")?.Split('-')?.Last();
        static bool IsInCluster = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST"));

        // Initialize service and storage clients
        static RegistryManager registryManager = RegistryManager.CreateFromConnectionString(IotHubConnectionString);
        static ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(IotHubConnectionString);
        static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
        static CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
        static CloudBlobContainer container = blobClient.GetContainerReference(BlobContainerName);

        private static ECDsa?_signingPrivateKey = null;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting...");
            _signingPrivateKey = await GetSigningPrivateKeyAsync();
            await StartEventProcessorHostAsync();
        }

        private static async Task<ECDsa> GetSigningPrivateKeyAsync()
        {
            Console.WriteLine("Loading signing crypto key...");
            string? privateKeyPem = null;
            if (IsInCluster)
            {
                privateKeyPem = Environment.GetEnvironmentVariable("SIGNING_PEM");
                if(!string.IsNullOrEmpty(privateKeyPem)) {
                    privateKeyPem = Encoding.UTF8.GetString(Convert.FromBase64String(privateKeyPem));
                    Console.WriteLine($"Key Base64 decoded layer 1");
                } else {
                    Console.WriteLine("In kube run-time - loading crypto from the secret in the local namespace.");
                    string secretName = "IoTTwinSecret";
                    string secretKey = "signKey";

                    if (string.IsNullOrEmpty(secretName) || string.IsNullOrEmpty(secretKey))
                    {
                        throw new InvalidOperationException("Private key secret name and secret key must be set.");
                    }
                    privateKeyPem = await GetPrivateKeyFromK8sSecretAsync(secretName, secretKey);
                }
            }
            else
            {
                Console.WriteLine("Not in kube run-time - loading crypto from the local storage.");
                // Load the private key from a local file when running locally
                privateKeyPem = await File.ReadAllTextAsync("dbg/sign-privkey.pem");
            }

            return LoadPrivateKeyFromPem(privateKeyPem);
        }

        private static async Task<string> GetPrivateKeyFromK8sSecretAsync(string secretName, string secretKey, string? secretNamespace = null)
        {
            Console.WriteLine($"GetPrivateKeyFromK8sSecretAsync {secretName}, {secretKey}, {secretNamespace}");
            var config = KubernetesClientConfiguration.BuildDefaultConfig();
            var k8sClient = new Kubernetes(config);
            Console.WriteLine($"Got k8s client in namespace {config.Namespace}");
            
            var ns = String.IsNullOrEmpty(secretNamespace) ? config.Namespace : secretNamespace;
            var secrets = await k8sClient.ListNamespacedSecretAsync(ns);
            
            Console.WriteLine($"Secrets in namespace '{ns}':");
            foreach (var secret in secrets.Items)
            {
                Console.WriteLine($"- {secret.Metadata.Name}");
            }
            
            var targetSecret = await k8sClient.ReadNamespacedSecretAsync(secretName, ns);
            Console.WriteLine($"Got k8s secret");

            if (targetSecret.Data.TryGetValue(secretKey, out var privateKeyBytes))
            {
                Console.WriteLine($"Got k8s secret bytes");
                return Encoding.UTF8.GetString(privateKeyBytes);
            }

            throw new Exception("Private key not found in the Kubernetes secret.");
        }

        private static ECDsa LoadPrivateKeyFromPem(string pemContent)
        {
            Console.WriteLine($"Loading key from PEM...");
            var privateKeyContent = pemContent.Replace("-----BEGIN EC PRIVATE KEY-----", "")
                                            .Replace("-----END EC PRIVATE KEY-----", "")
                                            .Replace("-----BEGIN PRIVATE KEY-----", "")
                                            .Replace("-----END PRIVATE KEY-----", "")
                                            .Replace("\n", "")
                                            .Replace("\r", "")
                                            .Trim();
            var privateKeyBytes = Convert.FromBase64String(privateKeyContent);
            Console.WriteLine($"Key Base64 decoded");
            var keyReader = new ReadOnlySpan<byte>(privateKeyBytes);
            ECDsa ecdsa = ECDsa.Create();
            ecdsa.ImportPkcs8PrivateKey(keyReader, out _);
            Console.WriteLine($"Imported private key");
            return ecdsa;
        }

        // Main method for the Firmware Update Backend
        // Method to start the EventProcessorHost for processing messages
        private static async Task StartEventProcessorHostAsync()
        {
            // Initialize the EventProcessorHost with necessary parameters
            EventProcessorHost eventProcessorHost = new EventProcessorHost(
                Guid.NewGuid().ToString(),
                EventHubCompatiblePath,
                PartitionReceiver.DefaultConsumerGroupName,
                EventHubCompatibleEndpoint,
                StorageConnectionString,
                BlobContainerName);

            Console.WriteLine("Registering EventProcessor...");
            var eventProcessorOptions = new EventProcessorOptions
            {
                MaxBatchSize = 100,
                PrefetchCount = 10,
                ReceiveTimeout = TimeSpan.FromSeconds(40),
                InvokeProcessorAfterReceiveTimeout = true,
            };

            // Configure initial offset provider if PartitionId is set
            if (!string.IsNullOrEmpty(PartitionId))
            {
                eventProcessorOptions.InitialOffsetProvider = (pid) => int.Parse(pid) == int.Parse(PartitionId) ? EventPosition.FromStart() : null;
            }
            // Register the event processor
            await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>(eventProcessorOptions);

            Console.WriteLine("Receiving. Press Ctrl+C to stop.");
            var cts = new CancellationTokenSource(); 
            Console.CancelKeyPress += (sender, e) => { cts.Cancel(); }; 
            await Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { }); // Wait indefinitely until the token is cancelled 
            Console.WriteLine("Bailed out.");

            // Unregister the event processor
            await eventProcessorHost.UnregisterEventProcessorAsync();
        }

        // Custom Event Processor class that implements IEventProcessor
        public class SimpleEventProcessor : IEventProcessor
        {
            public Task OpenAsync(PartitionContext context)
            {
                Console.WriteLine($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
                return Task.CompletedTask;
            }

            public Task CloseAsync(PartitionContext context, CloseReason reason)
            {
                Console.WriteLine($"SimpleEventProcessor closing. Partition: '{context.PartitionId}', Reason: '{reason}'");
                return Task.CompletedTask;
            }

            public Task ProcessErrorAsync(PartitionContext context, Exception error)
            {
                Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error}");
                return Task.CompletedTask;
            }

            // Process events received from the Event Hubs
            public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
            {
                Console.WriteLine($"SimpleEventProcessor ProcessEventsAsync. Partition: '{context.PartitionId}'.");
                foreach (var eventData in messages)
                {
                    try {
                        // Ignore messages older than 1 hour
                        if (DateTime.UtcNow - eventData.SystemProperties.EnqueuedTimeUtc > TimeSpan.FromHours(1))
                        {
                            Console.WriteLine("Ignoring message older than 1 hour.");
                            continue;
                        }

                        var data = Encoding.UTF8.GetString(eventData!.Body!.Array!, eventData.Body.Offset, eventData.Body.Count);
                        var eventDataJson = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(data)!;
                        if(eventDataJson == null || !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("DRAIN_D2C_QUEUES"))) {
                            Console.WriteLine($"Draining on Partition: {context.PartitionId}, Event: {data}");
                            continue;
                        }

                        string? deviceId = eventData.SystemProperties["iothub-connection-device-id"]?.ToString();
                        // Check if the received message is a FirmwareUpdateReady event
                        if (!String.IsNullOrEmpty(deviceId) && eventDataJson != null && eventDataJson.ContainsKey("event_type"))
                        {
                            // Check if the received message is a FirmwareUpdateReady event
                            if (eventDataJson["event_type"].ToString() == "FirmwareUpdateReady")
                            {
                                string filename = eventDataJson["filename"]?.ToString()!;
                                int chunkSize = int.Parse((eventDataJson["chunk_size"]).ToString()!);
                                long startFromPos = long.Parse((eventDataJson["start_from"] ?? "0").ToString()!);
                                await SendFirmwareUpdateAsync(deviceId, filename, chunkSize, startFromPos);
                            } else 
                            if (eventDataJson["event_type"].ToString() == "SignTwinKey")
                            {
                                string keyJPath = eventDataJson["keyPath"].ToString()!;
                                string atSignatureKey = eventDataJson["signatureKey"].ToString()!;
                                await createTwinKeySignature(deviceId, keyJPath, atSignatureKey);
                            }
                        }
                    } catch(Exception x) {
                        Console.WriteLine($"Failed parsing message on Partition: {context.PartitionId}, Error: {x.Message} - Ignoring");
                    }
                }
                await context.CheckpointAsync();
            }

        }

        // Send the firmware update to the specified device
        private static async Task SendFirmwareUpdateAsync(string deviceId, string filename, int chunkSize, long startFromPos = 0)
        {
            try
            {
                int maxEncodedChunkSize = 65535;
                int reservedOverhead = 500;
                int maxChunkSizeBeforeEncoding = (int)((maxEncodedChunkSize - reservedOverhead) * (3.0 / 4.0));
                chunkSize = Math.Min(chunkSize, maxChunkSizeBeforeEncoding);

                CloudBlockBlob blockBlob = container.GetBlockBlobReference(filename);
                await blockBlob.FetchAttributesAsync();
                long blobSize = blockBlob.Properties.Length;
                int totalChunks = (int)Math.Ceiling((double)blobSize / chunkSize);

                // Iterate through the chunks and send them as messages to the device
                for (int chunkIndex = (int)(startFromPos / chunkSize); chunkIndex < totalChunks; chunkIndex++)
                {
                    int offset = chunkIndex * chunkSize;
                    int length = (chunkIndex == totalChunks - 1) ? (int)(blobSize - offset) : chunkSize;

                    byte[] data = new byte[length];
                    await blockBlob.DownloadRangeToByteArrayAsync(data, 0, offset, length);

                    var messagePayload = new
                    {
                        filename = filename,
                        write_position = offset,
                        data = Convert.ToBase64String(data)
                    };

                    string c2dMessageJson = JsonConvert.SerializeObject(messagePayload);
                    var c2dMessage = new Message(Encoding.UTF8.GetBytes(c2dMessageJson))
                    {
                        MessageId = $"{filename}_{chunkIndex}",
                        ExpiryTimeUtc = DateTime.UtcNow.AddHours(1)
                    };

                    c2dMessage.Properties["chunk_index"] = chunkIndex.ToString();
                    c2dMessage.Properties["total_chunks"] = totalChunks.ToString();

                    // Send the message and handle the device's maximum queue depth exceeded exception
                    while (true)
                    {
                        try
                        {
                            Console.WriteLine($"=> {deviceId}: {ObfuscateDataField(c2dMessageJson)}");
                            await serviceClient.SendAsync(deviceId, c2dMessage);
                            break; // Succeeded
                        }
                        catch (Microsoft.Azure.Devices.Common.Exceptions.DeviceMaximumQueueDepthExceededException )
                        {
                            Console.WriteLine($"Overflow of 50 messages in the C2D queue, stalling until client '{deviceId}' unloads some.");
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                    }

                    // Add a short delay between sending messages to prevent overwhelming the device
                    // await Task.Delay(TimeSpan.FromMilliseconds(10));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error sending C2D message to device {deviceId}: {e}");
            }
        }


        async static Task createTwinKeySignature(string deviceId, string keyJPath, string atSignatureKey)
        {
            // Get the current device twin
            var twin = await registryManager.GetTwinAsync(deviceId);

            // Parse the JSON twin
            var twinJson = JObject.FromObject(twin.Properties.Desired);

            // Get the value at the specified JSON path
            var keyElement = twinJson.SelectToken(keyJPath);

            // var keyElement = twinJson.RootElement.SelectToken(keyJPath);

            if (keyElement == null)
            {
                throw new ArgumentException("Invalid JSON path specified");
            }

            // Sign the value using the ES512 algorithm
            var dataToSign = Encoding.UTF8.GetBytes(keyElement.ToString());
            var signature = _signingPrivateKey!.SignData(dataToSign, HashAlgorithmName.SHA512);

            // Convert the signature to a Base64 string
            var signatureString = Convert.ToBase64String(signature);

            // Add the signature to the JSON adjacent to the specified key
            // var parentJPath = keyJPath.Substring(0, keyJPath.LastIndexOf('.'));
            // var parentElement = string.IsNullOrEmpty(parentJPath) ? twinJson : keyElement.Parent.SelectToken(parentJPath);

            if(keyElement.Parent?.Parent != null)
                keyElement.Parent.Parent[atSignatureKey] = signatureString;

            // Update the device twin
            twin.Properties.Desired = new TwinCollection(twinJson.ToString());
            await registryManager.UpdateTwinAsync(deviceId, twin, twin.ETag);
        }

        // Obfuscate the data field in the JSON string for better logging
        private static string ObfuscateDataField(string jsonString)
        {
            var jsonObject = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
            if (jsonObject != null && jsonObject.ContainsKey("data"))
            {
                string data = jsonObject["data"].ToString()!;
                string obfuscatedData = data.Substring(0, Math.Min(data.Length, 10)) + "...";
                jsonObject["data"] = obfuscatedData;
            }
            return System.Text.Json.JsonSerializer.Serialize(jsonObject);
        }
    }
}
