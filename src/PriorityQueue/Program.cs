using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PriorityQueue.Services.Interfaces;
using PriorityQueue.Services;
using Shared.Logger;


var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("PriorityQueue", WebApplication.CreateBuilder(args));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IMessageProcessor, MessageProcessor>();

 var serviceBusConnectionString = Environment.GetEnvironmentVariable("SERVICE_BUS_CONNECTION_STRING") ??
                    "Endpoint=sb://tryperiodicqueue.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=XuvbHsyCDS74pTiizel0GjPLTNo8w8ftl+ASbDLFCpE=";
var serviceBusUrls = Environment.GetEnvironmentVariable("SERVICE_BUS_URLS")?.Split(';') ??
"periority1;periority2;periority3".Split(';');
var parallelCount = int.TryParse(Environment.GetEnvironmentVariable("PARALLEL_COUNT"), out int pCount) ? pCount : 1;

builder.Services.AddHostedService(sp => new InsertMsges(
    sp.GetRequiredService<IMessageProcessor>(),
    sp.GetRequiredService<ILoggerHandler>(),
    serviceBusConnectionString,
    new List<string>(serviceBusUrls),
    parallelCount));
builder.Services.AddHostedService(sp => new QueueConsumerService(
    sp.GetRequiredService<IMessageProcessor>(),
    sp.GetRequiredService<ILoggerHandler>(),
    serviceBusConnectionString,
    new List<string>(serviceBusUrls),
    parallelCount));


var app = builder.Build();
await app.RunAsync();
