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


        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<IHttpRequestorService, HttpRequestorService>();
        serviceCollection.AddScoped<IFirmwareUpdateService, FirmwareUpdateService>();
        serviceCollection.AddSingleton<ISigningService, SigningService>();

        serviceCollection.AddHttpClient();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var signingService = serviceProvider.GetService<ISigningService>();
        signingService.Init();

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
        var szureStreamProcessorFactory = new AzureStreamProcessorFactory(firmwareUpdateService, signingService);

        await eventProcessorHost.RegisterEventProcessorFactoryAsync(szureStreamProcessorFactory, eventProcessorOptions);

        Console.WriteLine("Receiving. Press Ctrl+C to stop.");
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) => { cts.Cancel(); };
        await Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { }); // Wait indefinitely until the token is cancelled 
        Console.WriteLine("Bailed out.");

        // Unregister the event processor
        await eventProcessorHost.UnregisterEventProcessorAsync();
    }
}