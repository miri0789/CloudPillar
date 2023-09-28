﻿using System.Runtime.Loader;
using common;
using Backend.Iotlistener.Interfaces;
using Backend.Iotlistener.Services;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Backend.Iotlistener.Wrappers;
using Backend.Iotlistener.Processors;

using Shared.Logger;


var builder = LoggerHostCreator.Configure("iotlistener", WebApplication.CreateBuilder(args));
// builder.Services.AddSingleton<ISchemaValidator, SchemaValidator>();
// builder.Services.AddScoped<IHttpRequestorService, HttpRequestorService>();
builder.Services.AddScoped<IFirmwareUpdateService, FirmwareUpdateService>();
builder.Services.AddScoped<ISigningService, SigningService>();
builder.Services.AddScoped<IStreamingUploadChunkService, StreamingUploadChunkService>();
builder.Services.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
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
var streamingUploadChunkEvent = app.Services.GetService<IStreamingUploadChunkService>();
var logger = app.Services.GetService<ILoggerHandler>();
var azureStreamProcessorFactory = new AzureStreamProcessorFactory(firmwareUpdateService, signingService, streamingUploadChunkEvent, _envirementVariable, logger, PartitionId);

await eventProcessorHost.RegisterEventProcessorFactoryAsync(azureStreamProcessorFactory, eventProcessorOptions);

await Task.Delay(Timeout.Infinite, cts.Token).ContinueWith(_ => { });

await eventProcessorHost.UnregisterEventProcessorAsync();
