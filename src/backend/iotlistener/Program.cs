using System.Runtime.Loader;
using common;
using iotlistener;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    public async static Task Main(string[] args)
    {
        string EventHubCompatiblePath = Environment.GetEnvironmentVariable(Constants.iothubEventHubCompatiblePath)!;
        string EventHubCompatibleEndpoint = Environment.GetEnvironmentVariable(Constants.iothubEventHubCompatibleEndpoint)!;
        string StorageConnectionString = Environment.GetEnvironmentVariable(Constants.storageConnectionString)!;
        string BlobContainerName = Environment.GetEnvironmentVariable(Constants.blobContainerName)!;
        string? PartitionId = Environment.GetEnvironmentVariable("PARTITION_ID")?.Split('-')?.Last();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<IHttpRequestorService, HttpRequestorService>();
        serviceCollection.AddScoped<IFirmwareUpdateService, FirmwareUpdateService>();
        serviceCollection.AddScoped<ISigningService, SigningService>();

        serviceCollection.AddHttpClient();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var cts = new CancellationTokenSource();
        AssemblyLoadContext.Default.Unloading += context =>
        {
            cts.Cancel();
        };

        EventProcessorHost eventProcessorHost = new EventProcessorHost(
                Guid.NewGuid().ToString(),
                EventHubCompatiblePath,
                PartitionReceiver.DefaultConsumerGroupName,
                EventHubCompatibleEndpoint,
                StorageConnectionString,
                BlobContainerName);

        var eventProcessorOptions = new EventProcessorOptions
        {
            MaxBatchSize = 100,
            PrefetchCount = 10,
            ReceiveTimeout = TimeSpan.FromSeconds(40),
            InvokeProcessorAfterReceiveTimeout = true,
        };


        var firmwareUpdateService = serviceProvider.GetService<IFirmwareUpdateService>();
        var signingService = serviceProvider.GetService<ISigningService>();
        var azureStreamProcessorFactory = new AzureStreamProcessorFactory(firmwareUpdateService, signingService, PartitionId);

        await eventProcessorHost.RegisterEventProcessorFactoryAsync(azureStreamProcessorFactory, eventProcessorOptions);

        await Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { });

        await eventProcessorHost.UnregisterEventProcessorAsync();
    }
}