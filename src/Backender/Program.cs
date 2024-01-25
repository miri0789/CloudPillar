using System.Reflection;
using Backender.Services.Interfaces;
using Backender.Services;
using Shared.Logger;
using Backender.Wrappers.Interfaces;
using Backender.Wrappers;


var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("Backender", WebApplication.CreateBuilder(args));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IMessageProcessor, MessageProcessor>();
builder.Services.AddScoped<IEnvironmentsWrapper, EnvironmentsWrapper>();


builder.Services.AddHostedService(sp => new QueueConsumerService(
    sp.GetRequiredService<IMessageProcessor>(),
    sp.GetRequiredService<ILoggerHandler>(),
    sp.GetRequiredService<IEnvironmentsWrapper>()));

var app = builder.Build();
await app.RunAsync();
