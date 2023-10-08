using System.Reflection;
using Shared.Logger;
using Shared.Entities.Factories;
using Shared.Entities.Services;
using Backend.Iotlistener.Wrappers;
using Backend.Iotlistener.Interfaces;
using Backend.Iotlistener.Services;
using Backend.Iotlistener.Processors;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System.Runtime.Loader;

var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("Iotlistener1", WebApplication.CreateBuilder(args));

builder.Services.AddScoped<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<IFirmwareUpdateService, FirmwareUpdateService>();
builder.Services.AddScoped<ISigningService, SigningService>();
builder.Services.AddScoped<IStreamingUploadChunkService, StreamingUploadChunkService>();

var app = builder.Build();
app.Run();

var logger = app.Services.GetRequiredService<ILoggerHandler>();
logger.Info($"Informational Version: {informationalVersion ?? "Unknown"}");


var cts = new CancellationTokenSource();
AssemblyLoadContext.Default.Unloading += context =>
{
    cts.Cancel();
};
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
var streamingUploadChunkEvent = app.Services.GetService<IStreamingUploadChunkService>();
var azureStreamProcessorFactory = new AzureStreamProcessorFactory(firmwareUpdateService, signingService, streamingUploadChunkEvent, _envirementVariable, PartitionId);

await eventProcessorHost.RegisterEventProcessorFactoryAsync(azureStreamProcessorFactory, eventProcessorOptions);

await Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { });

await eventProcessorHost.UnregisterEventProcessorAsync();


