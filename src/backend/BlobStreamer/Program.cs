using Backend.BlobStreamer.Services;
using Backend.BlobStreamer.Wrappers;
using System.Reflection;
using Shared.Logger;
using Shared.Entities.Factories;
using Shared.Entities.Services;
using Backend.BlobStreamer.Wrappers.Interfaces;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.Infra.Common.Wrappers;
using Backend.BlobStreamer.Services.Interfaces;
using Backend.Infra.Wrappers;
using Backend.Infra.Common;
var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("blobstreamer", WebApplication.CreateBuilder(args));

builder.Services.AddScoped<ICloudStorageWrapper, CloudStorageWrapper>();
builder.Services.AddScoped<IRegistryManagerWrapper, RegistryManagerWrapper>();
builder.Services.AddScoped<Backend.BlobStreamer.Wrappers.Interfaces.IEnvironmentsWrapper, Backend.BlobStreamer.Wrappers.EnvironmentsWrapper>();
builder.Services.AddScoped<Backend.Infra.Common.Wrappers.Interfaces.IEnvironmentsWrapper, Backend.Infra.Common.Wrappers.EnvironmentsWrapper>();
builder.Services.AddScoped<ICloudBlockBlobWrapper, CloudBlockBlobWrapper>();
builder.Services.AddScoped<IMessageFactory, MessageFactory>();
builder.Services.AddScoped<IBlobService, BlobService>();
builder.Services.AddScoped<IUploadStreamChunksService, UploadStreamChunksService>();
builder.Services.AddScoped<ICheckSumService, CheckSumService>();
builder.Services.AddScoped<ICommonEnvironmentsWrapper, CommonEnvironmentsWrapper>();
builder.Services.AddScoped<IDeviceConnectService, DeviceConnectService>();
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
