using System.Runtime.Loader;
using common;
using iotlistener;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;

using Shared.Logger;

class Program
{
    public async static Task Main(string[] args)
    {
        string EventHubCompatiblePath = Environment.GetEnvironmentVariable(Constants.iothubEventHubCompatiblePath)!;
        string EventHubCompatibleEndpoint = Environment.GetEnvironmentVariable(Constants.iothubEventHubCompatibleEndpoint)!;
        string StorageConnectionString = Environment.GetEnvironmentVariable(Constants.storageConnectionString)!;
        string BlobContainerName = Environment.GetEnvironmentVariable(Constants.blobContainerName)!;
        string? PartitionId = Environment.GetEnvironmentVariable("PARTITION_ID")?.Split('-')?.Last();

        var builder = LoggerHostCreator.Configure("iotlistener", WebApplication.CreateBuilder(args));

        builder.Services.AddSingleton<ISchemaValidator, SchemaValidator>();
        builder.Services.AddScoped<IHttpRequestorService, HttpRequestorService>();
        builder.Services.AddScoped<IFirmwareUpdateService, FirmwareUpdateService>();
        builder.Services.AddScoped<ISigningService, SigningService>();
        builder.Services.AddHttpClient();

        var cts = new CancellationTokenSource();
        AssemblyLoadContext.Default.Unloading += context =>
        {
            cts.Cancel();
        };

        /*EventProcessorHost eventProcessorHost = new EventProcessorHost(
                Guid.NewGuid().ToString(),
                EventHubCompatiblePath,
                PartitionReceiver.DefaultConsumerGroupName,
                EventHubCompatibleEndpoint,
                StorageConnectionString,
                BlobContainerName);*/

        var eventProcessorOptions = new EventProcessorOptions
        {
            MaxBatchSize = 100,
            PrefetchCount = 10,
            ReceiveTimeout = TimeSpan.FromSeconds(40),
            InvokeProcessorAfterReceiveTimeout = true,
        };

        var app = builder.Build();
        var firmwareUpdateService = app.Services.GetService<IFirmwareUpdateService>();
        var signingService = app.Services.GetService<ISigningService>();
        var logger = app.Services.GetService<ILoggerHandler>();
        var azureStreamProcessorFactory = new AzureStreamProcessorFactory(firmwareUpdateService, signingService, logger, PartitionId);

        //await eventProcessorHost.RegisterEventProcessorFactoryAsync(azureStreamProcessorFactory, eventProcessorOptions);

        await Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { });

        //await eventProcessorHost.UnregisterEventProcessorAsync();
    }
}