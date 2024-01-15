using System.Reflection;
using PriorityQueue.Services.Interfaces;
using PriorityQueue.Services;
using Shared.Logger;
using PriorityQueue.Wrappers.Interfaces;
using PriorityQueue.Wrappers;


var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("PriorityQueue", WebApplication.CreateBuilder(args));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IMessageProcessor, MessageProcessor>();
builder.Services.AddScoped<IEnvironmentsWrapper, EnvironmentsWrapper>();


builder.Services.AddHostedService(sp => new QueueConsumerService(
    sp.GetRequiredService<IMessageProcessor>(),
    sp.GetRequiredService<ILoggerHandler>(),
    sp.GetRequiredService<IEnvironmentsWrapper>()));


var app = builder.Build();
await app.RunAsync();
