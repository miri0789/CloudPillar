using Backend.Keyholder.Interfaces;
using Backend.Keyholder.Services;
using Backend.Keyholder.Wrappers;
using System.Reflection;
using Shared.Logger;
using Backend.Keyholder.Wrappers.Interfaces;
using Shared.Entities.Factories;
using Backend.Infra.Wrappers;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Services;
using Backend.Infra.Common.Wrappers;

var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("keyholder", WebApplication.CreateBuilder(args));

builder.Services.AddScoped<ISigningService, SigningService>();
builder.Services.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IIndividualEnrollmentWrapper, IndividualEnrollmentWrapper>();
builder.Services.AddScoped<IMessageFactory, MessageFactory>();
builder.Services.AddScoped<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddScoped<ICommonEnvironmentsWrapper, CommonEnvironmentsWrapper>();
builder.Services.AddScoped<IDeviceConnectService, DeviceConnectService>();
builder.Services.AddScoped<IX509CertificateWrapper, X509CertificateWrapper>();
builder.Services.AddScoped<IProvisioningServiceClientWrapper, ProvisioningServiceClientWrapper>();
builder.Services.AddScoped<IRegistryManagerWrapper, RegistryManagerWrapper>();

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
