
using System.Reflection;
using Backend.BEApi.Services;
using Backend.BEApi.Services.Interfaces;
using Backend.Infra.Common.Wrappers;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.Infra.Wrappers;
using Shared.Logger;

var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("beapi", WebApplication.CreateBuilder(args));

builder.Services.AddScoped<ICommonEnvironmentsWrapper, CommonEnvironmentsWrapper>();
builder.Services.AddScoped<IRegistryManagerWrapper, RegistryManagerWrapper>();
builder.Services.AddScoped<IValidateCertificateService, ValidateCertificateService>();

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