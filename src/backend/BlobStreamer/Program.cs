using Backend.BlobStreamer.Services;
using Backend.BlobStreamer.Interfaces;
using Backend.BlobStreamer.Wrappers;
using System.Reflection;
using Shared.Logger;
using Shared.Entities.Factories;
using Backend.Infra.Common;
using Shared.Entities.Services;

var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("blobstreamer", WebApplication.CreateBuilder(args));

builder.Services.AddScoped<ICloudStorageWrapper, CloudStorageWrapper>();
builder.Services.AddScoped<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddScoped<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<ICloudBlockBlobWrapper, CloudBlockBlobWrapper>();
builder.Services.AddScoped<IMessageFactory, MessageFactory>();
builder.Services.AddScoped<IBlobService, BlobService>();
builder.Services.AddScoped<IUploadStreamChunksService, UploadStreamChunksService>();
builder.Services.AddScoped<ICheckSumService, CheckSumService>();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerHandler>();
logger.Info($"Informational Version: {informationalVersion ?? "Unknown"}");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
