using System.Reflection;
using Shared.Logger;
using Backend.BEApi.Services;
using Backend.BEApi.Services.Interfaces;
using Backend.BEApi.Wrappers;
using Backend.BEApi.Wrappers.Interfaces;
using Backend.Infra.Common.Services;
using Backend.Infra.Common.Services.Interfaces;
using Backend.Infra.Common.Wrappers;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.Infra.Wrappers;
using Shared.Entities.Factories;
using Backend.BEApi.Services.interfaces;

var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;

var builder = LoggerHostCreator.Configure("beapi", WebApplication.CreateBuilder(args));

builder.Services.AddScoped<IMessageFactory, MessageFactory>();
builder.Services.AddScoped<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddScoped<IDeviceConnectService, DeviceConnectService>();
builder.Services.AddScoped<IIndividualEnrollmentWrapper, IndividualEnrollmentWrapper>();
builder.Services.AddScoped<IX509CertificateWrapper, X509CertificateWrapper>();
builder.Services.AddScoped<IProvisioningServiceClientWrapper, ProvisioningServiceClientWrapper>();
builder.Services.AddScoped<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<ICommonEnvironmentsWrapper, CommonEnvironmentsWrapper>();
builder.Services.AddScoped<IRegistryManagerWrapper, RegistryManagerWrapper>();
builder.Services.AddScoped<ICloudStorageWrapper, CloudStorageWrapper>();
builder.Services.AddScoped<ICloudBlockBlobWrapper, CloudBlockBlobWrapper>();
builder.Services.AddScoped<IChangeSpecService, ChangeSpecService>();
builder.Services.AddScoped<ITwinDiseredService, TwinDiseredService>();
builder.Services.AddScoped<ICertificateIdentityService, CertificateIdentityService>();
builder.Services.AddScoped<IDeviceCertificateService, DeviceCertificateService>();
builder.Services.AddScoped<ISchemaValidator, SchemaValidator>();
builder.Services.AddScoped<IHttpRequestorService, HttpRequestorService>();
builder.Services.AddScoped<ISHA256Wrapper, SHA256Wrapper>();

builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerHandler>();
logger.Info($"Informational Version: {informationalVersion ?? "Unknown"}");

app.UseSwagger();
app.UseSwaggerUI();


app.MapControllers();

app.Run();