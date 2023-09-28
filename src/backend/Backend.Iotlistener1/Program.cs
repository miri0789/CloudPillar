using Backend.BlobStreamer.Services;
using Backend.BlobStreamer.Interfaces;
using Backend.BlobStreamer.Wrappers;
using System.Reflection;
using Shared.Logger;
using Shared.Entities.Factories;
using Shared.Entities.Services;

var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("iotlistener", WebApplication.CreateBuilder(args));



var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerHandler>();
logger.Info($"Informational Version: {informationalVersion ?? "Unknown"}");



app.Run();
