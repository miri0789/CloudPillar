var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("blobstreamer", WebApplication.CreateBuilder(args));

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerHandler>();
logger.Info($"Informational Version: {informationalVersion ?? "Unknown"}");