using System.Runtime.Loader;
using common;
using Backend.Iotlistener;
using Backend.Iotlistener.Interfaces;
using Backend.Iotlistener.Services;
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

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ISchemaValidator, SchemaValidator>();
        serviceCollection.AddScoped<IHttpRequestorService, HttpRequestorService>();
        serviceCollection.AddScoped<IFirmwareUpdateService, FirmwareUpdateService>();
        serviceCollection.AddScoped<ISigningService, SigningService>();
        serviceCollection.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
        var builder = LoggerHostCreator.Configure("iotlistener", WebApplication.CreateBuilder(args));
        builder.Services.AddHttpClient();

        var cts = new CancellationTokenSource();
        AssemblyLoadContext.Default.Unloading += context =>
        {
            cts.Cancel();
        };
        var app = builder.Build();

        var _envirementVariable = app.Services.GetService<IEnvironmentsWrapper>();

        string? PartitionId = _envirementVariable.partitionId?.Split('-')?.Last();

        EventProcessorHost eventProcessorHost = new EventProcessorHost(
                Guid.NewGuid().ToString(),
                _envirementVariable.iothubEventHubCompatiblePath,
                PartitionReceiver.DefaultConsumerGroupName,
                _envirementVariable.iothubEventHubCompatibleEndpoint,
                _envirementVariable.storageConnectionString,
                _envirementVariable.blobContainerName);

        var eventProcessorOptions = new EventProcessorOptions
        {
            MaxBatchSize = 100,
            PrefetchCount = 10,
            ReceiveTimeout = TimeSpan.FromSeconds(40),
            InvokeProcessorAfterReceiveTimeout = true,
        };

        var firmwareUpdateService = app.Services.GetService<IFirmwareUpdateService>();
        var signingService = app.Services.GetService<ISigningService>();
        var logger = app.Services.GetService<ILoggerHandler>();
        var azureStreamProcessorFactory = new AzureStreamProcessorFactory(firmwareUpdateService, signingService, _envirementVariable, logger, PartitionId);

        await eventProcessorHost.RegisterEventProcessorFactoryAsync(azureStreamProcessorFactory, eventProcessorOptions);

        await Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { });

        await eventProcessorHost.UnregisterEventProcessorAsync();
    }
}