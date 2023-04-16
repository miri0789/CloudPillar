using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
// using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Devices;
using Newtonsoft.Json;
// using ConsumerPartitionContext = Azure.Messaging.EventHubs.Consumer.PartitionContext;


namespace FirmwareUpdate
{
    class Program
    {
        static string IotHubConnectionString = Environment.GetEnvironmentVariable("IOTHUB_CONNECTION_STRING");
        static string StorageConnectionString = Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING");
        static string BlobContainerName = Environment.GetEnvironmentVariable("BLOB_CONTAINER_NAME");
        static string EventHubCompatibleEndpoint = Environment.GetEnvironmentVariable("IOTHUB_EVENT_HUB_COMPATIBLE_ENDPOINT");
        static string EventHubCompatiblePath = Environment.GetEnvironmentVariable("IOTHUB_EVENT_HUB_COMPATIBLE_PATH");
        // static string iotHubSharedAccessKeyName = Environment.GetEnvironmentVariable("IOTHUB_SHARED_ACCESS_KEY_NAME");
        // static string iotHubSharedAccessKey = Environment.GetEnvironmentVariable("IOTHUB_SHARED_ACCESS_KEY");

        // static RegistryManager registryManager = RegistryManager.CreateFromConnectionString(IotHubConnectionString);
        static ServiceClient serviceClient = ServiceClient.CreateFromConnectionString(IotHubConnectionString);
        static CloudStorageAccount storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
        static CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
        static CloudBlobContainer container = blobClient.GetContainerReference(BlobContainerName);

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting...");
            await StartEventProcessorHostAsync();
        }

        // private static async Task StartEventProcessorHostAsync()
        // {
        //     string consumerGroupName = EventHubConsumerClient.DefaultConsumerGroupName;
        //     string storageContainerName = BlobContainerName;
        //     string leaseContainerName = "leases";

        //     CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

        //     var storageContainer = blobClient.GetContainerReference(storageContainerName);
        //     await storageContainer.CreateIfNotExistsAsync();

        //     var leaseContainer = blobClient.GetContainerReference(leaseContainerName);
        //     await leaseContainer.CreateIfNotExistsAsync();

        //     var eventProcessorHost = new EventProcessorHost(
        //         EventHubCompatiblePath,
        //         consumerGroupName,
        //         IotHubConnectionString,
        //         StorageConnectionString,
        //         leaseContainerName);

        //     await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>();

        //     Console.WriteLine("Press Ctrl+C to stop.");
        //     await Task.Delay(TimeSpan.FromDays(1));
        // }

        private static async Task StartEventProcessorHostAsync()
        {
            string eventHubsCompatibleConnectionString = EventHubCompatibleEndpoint;

            EventProcessorHost eventProcessorHost = new EventProcessorHost(
                Guid.NewGuid().ToString(),
                EventHubCompatiblePath,
                PartitionReceiver.DefaultConsumerGroupName,
                eventHubsCompatibleConnectionString,
                StorageConnectionString,
                BlobContainerName);

            // Register the event processor
            await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>();

            Console.WriteLine("Receiving. Press enter key to stop.");
            Console.ReadLine();

            // Unregister the event processor
            await eventProcessorHost.UnregisterEventProcessorAsync();
        }


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
            // public async Task CloseAsync(PartitionContext context, CloseReason reason)
            // {
            //     Console.WriteLine($"Processor shutting down. Partition: '{context.PartitionId}', Reason: '{reason}'.");
            // }

            // public Task OpenAsync(PartitionContext context)
            // {
            //     Console.WriteLine($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
            //     return Task.CompletedTask;
            // }

            // public async Task ProcessErrorAsync(PartitionContext context, Exception error)
            // {
            //     Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
            // }

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
                    // var eventDataJson = JsonSerializer.Deserialize<Dictionary<string, object>>(data);
                    var eventDataJson = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(data);

                    if (eventDataJson.ContainsKey("event_type") && eventDataJson["event_type"].ToString() == "FirmwareUpdateReady")
                    {
                        string deviceId = eventData.SystemProperties["iothub-connection-device-id"].ToString();
                        string filename = eventDataJson["filename"].ToString();
                        int chunkSize = int.Parse(eventDataJson["chunk_size"].ToString());
                        await SendFirmwareUpdateAsync(deviceId, filename, chunkSize);
                    }
                }
                await context.CheckpointAsync();
            }
        }

        private static async Task SendFirmwareUpdateAsync(string deviceId, string filename, int chunkSize)
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

                for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
                {
                    int offset = chunkIndex * chunkSize;
                    int length = (chunkIndex == totalChunks - 1) ? (int)(blobSize - offset) : chunkSize;

                    byte[] data = new byte[length];
                    await blockBlob.DownloadRangeToByteArrayAsync(data, 0, offset, length);

                    var messagePayload = new
                    {
                        filename = filename,
                        // chunk_index = chunkIndex,
                        write_position = offset,
                        // total_chunks = totalChunks,
                        // data = BitConverter.ToString(data).Replace("-", "").ToLower()
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

                    while(true) {
                        try {
                            Console.WriteLine($"=> {deviceId}: {ObfuscateDataField(c2dMessageJson)}");
                            await serviceClient.SendAsync(deviceId, c2dMessage);
                            break; //Succeeded
                        } catch (Microsoft.Azure.Devices.Common.Exceptions.DeviceMaximumQueueDepthExceededException x) {
                            Console.WriteLine("Overflow 50 messages, client must unload!");
                            await Task.Delay(TimeSpan.FromSeconds(3));
                        }
                    }   
                    // await registryManager.SendAsync(deviceId, new Microsoft.Azure.Devices.Message(Encoding.UTF8.GetBytes(c2dMessageJson)));
                    // await registryManager.SendCloudToDeviceMessageAsync(deviceId, c2dMessage);
                    // Console.WriteLine($"Sent C2D message to device {deviceId}");
                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error sending C2D message to device {deviceId}: {e}");
            }
        }

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
