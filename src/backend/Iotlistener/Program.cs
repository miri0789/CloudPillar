using System.Reflection;
using Shared.Logger;
using Backend.Iotlistener.Wrappers;
using Backend.Iotlistener.Interfaces;
using Backend.Iotlistener.Services;
using Backend.Iotlistener.Processors;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System.Runtime.Loader;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Services;

var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("Iotlistener", WebApplication.CreateBuilder(args));

builder.Services.AddScoped<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<IQueueMessagesService, QueueMessagesService>();
builder.Services.AddScoped<IFileDownloadService, FileDownloadService>();
builder.Services.AddScoped<IProvisionDeviceService, ProvisionDeviceService>();
builder.Services.AddScoped<ISigningService, SigningService>();
builder.Services.AddScoped<IStreamingUploadChunkService, StreamingUploadChunkService>();
builder.Services.AddScoped<ISchemaValidator, SchemaValidator>();
builder.Services.AddScoped<IHttpRequestorService, HttpRequestorService>();
builder.Services.AddHttpClient();
var app = builder.Build();

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

var FileDownloadService = app.Services.GetService<IFileDownloadService>();
var signingService = app.Services.GetService<ISigningService>();
var streamingUploadChunkEvent = app.Services.GetService<IStreamingUploadChunkService>();
var provisionDeviceCertificateService = app.Services.GetService<IProvisionDeviceService>();
var azureStreamProcessorFactory = new AzureStreamProcessorFactory(FileDownloadService, signingService, streamingUploadChunkEvent, provisionDeviceCertificateService, _envirementVariable, logger, PartitionId);

await eventProcessorHost.RegisterEventProcessorFactoryAsync(azureStreamProcessorFactory, eventProcessorOptions);

await Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { });

await eventProcessorHost.UnregisterEventProcessorAsync();


app.Run();
