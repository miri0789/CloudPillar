using System.Reflection;
using Shared.Logger;
using Shared.Entities.Factories;
using Shared.Entities.Services;
using Backend.Infra.Common;
using Backend.Infra.Common.Wrappers;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.BlobStreamer.Wrappers;
using Backend.BlobStreamer.Wrappers.Interfaces;
using Backend.BlobStreamer.Services;
using Backend.BlobStreamer.Services.Interfaces;
using Backend.Infra.Wrappers;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Services;

var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("blobstreamer", WebApplication.CreateBuilder(args));

builder.Services.AddScoped<ICloudStorageWrapper, CloudStorageWrapper>();
builder.Services.AddScoped<IRegistryManagerWrapper, RegistryManagerWrapper>();
builder.Services.AddScoped<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<ICloudBlockBlobWrapper, CloudBlockBlobWrapper>();
builder.Services.AddScoped<IMessageFactory, MessageFactory>();
builder.Services.AddScoped<IBlobService, BlobService>();
builder.Services.AddScoped<ITwinDiseredService, TwinDiseredService>();
builder.Services.AddScoped<IUploadStreamChunksService, UploadStreamChunksService>();
builder.Services.AddScoped<ICheckSumService, CheckSumService>();
builder.Services.AddScoped<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddScoped<ICommonEnvironmentsWrapper, CommonEnvironmentsWrapper>();
builder.Services.AddScoped<IDeviceConnectService, DeviceConnectService>();
builder.Services.AddScoped<IChangeSpecService, ChangeSpecService>();
builder.Services.AddScoped<ISchemaValidator, SchemaValidator>();
builder.Services.AddScoped<IHttpRequestorService, HttpRequestorService>();

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UsePathBase(new PathString(CommonConstants.BLOBSTREAMER_BASE_URL));

var logger = app.Services.GetRequiredService<ILoggerHandler>();
logger.Info($"Informational Version: {informationalVersion ?? "Unknown"}");

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
