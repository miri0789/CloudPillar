using System.Reflection;
using Shared.Logger;
using Shared.Entities.Factories;
using Shared.Entities.Services;
using Backend.Iotlistener.Wrappers;
using Backend.Iotlistener.Interfaces;

var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("Iotlistener1", WebApplication.CreateBuilder(args));

builder.Services.AddScoped<IEnvironmentsWrapper, EnvironmentsWrapper>();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerHandler>();
logger.Info($"Informational Version: {informationalVersion ?? "Unknown"}");


app.Run();
