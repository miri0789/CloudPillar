using System.Text;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;

namespace FirmwareUpdate
{
    // Main class for the Firmware Update Backend
    class Program
    {
        static string IotHubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        static string StorageConnectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING");
        static string BlobContainerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME");
        static string EventHubCompatibleEndpoint = Environment.GetEnvironmentVariable("IOTHUB_EVENT_HUB_COMPATIBLE_ENDPOINT");
        static string EventHubCompatiblePath = Environment.GetEnvironmentVariable("IOTHUB_EVENT_HUB_COMPATIBLE_PATH");
        static string PartitionId = Environment.GetEnvironmentVariable("PARTITION_ID")?.Split('-')?.Last();

        // Initialize service and storage clients
        static ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(IotHubConnectionString);
        static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
        static CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
        static CloudBlobContainer container = blobClient.GetContainerReference(BlobContainerName);

        // Main method for the Firmware Update Backend
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting...");
            await StartEventProcessorHostAsync();
        }

        // Method to start the EventProcessorHost for processing messages
        private static async Task StartEventProcessorHostAsync()
        {
            string eventHubsCompatibleConnectionString = EventHubCompatibleEndpoint;

            // Initialize the EventProcessorHost with necessary parameters
            EventProcessorHost eventProcessorHost = new EventProcessorHost(
                Guid.NewGuid().ToString(),
                EventHubCompatiblePath,
                PartitionReceiver.DefaultConsumerGroupName,
                eventHubsCompatibleConnectionString,
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
                eventProcessorOptions.InitialOffsetProvider = (pid) => pid == PartitionId ? EventPosition.FromStart() : null;
            }
            // Register the event processor
            await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>(eventProcessorOptions);

            Console.WriteLine("Receiving. Press Ctrl+C to stop.");
            var cts = new CancellationTokenSource(); 
            Console.CancelKeyPress += (sender, e) => { cts.Cancel(); }; 
            await Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { }); 
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
                Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
                return Task.CompletedTask;
            }

            // Process events received from the Event Hubs
            public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
            {
                foreach (var eventData in messages)
                {
                    // Ignore messages older than 1 hour
                    if (DateTime.UtcNow - eventData.SystemProperties.EnqueuedTimeUtc > TimeSpan.FromHours(1))
                    {
                        Console.WriteLine("Ignoring message older than 1 hour.");
                        continue;
                    }

                    var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);
                    var eventDataJson = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(data);

                    // Check if the received message is a FirmwareUpdateReady event
                    if (eventDataJson.ContainsKey("event_type") && eventDataJson["event_type"].ToString() == "FirmwareUpdateReady")
                    {
                        string deviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();
                        string filename = eventDataJson["filename"].ToString();
                        int chunkSize = int.Parse(eventDataJson["chunk_size"].ToString());
                        long startFromPos = eventDataJson.ContainsKey("start_from") ? long.Parse(eventDataJson["start_from"].ToString()) : 0L;
                        await SendFirmwareUpdateAsync(deviceId, filename, chunkSize, startFromPos);
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
                        catch (Microsoft.Azure.Devices.Common.Exceptions.DeviceMaximumQueueDepthExceededException x)
                        {
                            Console.WriteLine($"Overflow 50 messages, client '{deviceId}' must unload!");
                            await Task.Delay(TimeSpan.FromSeconds(3));
                        }
                    }

                    // Add a short delay between sending messages to prevent overwhelming the device
                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error sending C2D message to device {deviceId}: {e}");
            }
        }

        // Obfuscate the data field in the JSON string for better logging
        private static string ObfuscateDataField(string jsonString)
        {
            var jsonObject = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonString);
            if (jsonObject.ContainsKey("data"))
            {
                string data = jsonObject["data"].ToString();
                string obfuscatedData = data.Substring(0, Math.Min(data.Length, 10)) + "...";
                jsonObject["data"] = obfuscatedData;
            }

            return System.Text.Json.JsonSerializer.Serialize(jsonObject);
        }
    }
}
