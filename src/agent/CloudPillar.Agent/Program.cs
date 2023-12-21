using System.Net;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Sevices;
using CloudPillar.Agent.Utilities;
using CloudPillar.Agent.Validators;
using CloudPillar.Agent.Wrappers;
using FluentValidation;
using Shared.Entities.Factories;
using Shared.Entities.Services;
using Shared.Entities.Twin;
using System.Runtime.InteropServices;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers.Interfaces;

bool runAsService = args.FirstOrDefault() == "--winsrv";
Environment.CurrentDirectory = Directory.GetCurrentDirectory();
if(!runAsService && args.FirstOrDefault()!=null)
{
    Environment.CurrentDirectory = args.FirstOrDefault();
}

const string MY_ALLOW_SPECIFICORIGINS = "AllowLocalhost";
var builder = LoggerHostCreator.Configure("Agent API", WebApplication.CreateBuilder(args));
var port = builder.Configuration.GetValue(Constants.CONFIG_PORT, Constants.HTTP_DEFAULT_PORT);
var httpsPort = builder.Configuration.GetValue(Constants.HTTPS_CONFIG_PORT, Constants.HTTPS_DEFAULT_PORT);
var httpUrl = $"http://localhost:{port}";
var httpsUrl = $"https://localhost:{httpsPort}";

var serviceName = builder.Configuration.GetValue("AgentServiceName", Constants.AGENT_SERVICE_DEFAULT_NAME);

if (runAsService && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddScoped<IWindowsServiceWrapper, WindowsServiceWrapper>();
    var windowsServiceWrapper = builder.Services.BuildServiceProvider().GetRequiredService<IWindowsServiceWrapper>();
    windowsServiceWrapper?.InstallWindowsService(serviceName, Environment.CurrentDirectory);
    Environment.Exit(0);
}

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = serviceName;
});

builder.Services.AddHostedService<StateMachineListenerService>();
builder.Services.AddSingleton<IStateMachineChangedEvent, StateMachineChangedEvent>();
builder.Services.AddSingleton<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<IDPSProvisioningDeviceClientHandler, X509DPSProvisioningDeviceClientHandler>();
builder.Services.AddScoped<IX509CertificateWrapper, X509CertificateWrapper>();
builder.Services.AddScoped<IMatcherWrapper, MatcherWrapper>();
builder.Services.AddScoped<IStrictModeHandler, StrictModeHandler>();
builder.Services.AddScoped<ISymmetricKeyProvisioningHandler, SymmetricKeyProvisioningHandler>();
builder.Services.AddScoped<IC2DEventSubscriptionSession, C2DEventSubscriptionSession>();
builder.Services.AddScoped<IMessageSubscriber, MessageSubscriber>();
builder.Services.AddScoped<ISignatureHandler, SignatureHandler>();
builder.Services.AddScoped<IMessageFactory, MessageFactory>();
builder.Services.AddScoped<ICheckSumService, CheckSumService>();
builder.Services.AddScoped<ITwinActionsHandler, TwinActionsHandler>();
builder.Services.AddScoped<ITwinHandler, TwinHandler>();
builder.Services.AddScoped<IFileDownloadHandler, FileDownloadHandler>();
builder.Services.AddScoped<ICloudBlockBlobWrapper, CloudBlockBlobWrapper>();
builder.Services.AddScoped<IFileStreamerWrapper, FileStreamerWrapper>();
builder.Services.AddScoped<ID2CMessengerHandler, D2CMessengerHandler>();
builder.Services.AddScoped<IStreamingFileUploaderHandler, StreamingFileUploaderHandler>();
builder.Services.AddScoped<IBlobStorageFileUploaderHandler, BlobStorageFileUploaderHandler>();
builder.Services.AddScoped<IFileUploaderHandler, FileUploaderHandler>();
builder.Services.AddScoped<IValidator<UpdateReportedProps>, UpdateReportedPropsValidator>();
builder.Services.AddScoped<IRuntimeInformationWrapper, RuntimeInformationWrapper>();
builder.Services.AddScoped<ISymmetricKeyWrapper, SymmetricKeyWrapper>();
builder.Services.AddScoped<IValidator<TwinDesired>, TwinDesiredValidator>();
builder.Services.AddScoped<IReprovisioningHandler, ReprovisioningHandler>();
builder.Services.AddScoped<ISHA256Wrapper, SHA256Wrapper>();
builder.Services.AddScoped<IProvisioningServiceClientWrapper, ProvisioningServiceClientWrapper>();
builder.Services.AddScoped<IProvisioningDeviceClientWrapper, ProvisioningDeviceClientWrapper>();
builder.Services.AddScoped<IMatcherWrapper, MatcherWrapper>();
builder.Services.AddScoped<IStateMachineHandler, StateMachineHandler>();
builder.Services.AddScoped<IRunDiagnosticsHandler, RunDiagnosticsHandler>();
builder.Services.AddScoped<IX509Provider, X509Provider>();

var signFileSettings = builder.Configuration.GetSection("SignFileSettings");
builder.Services.Configure<SignFileSettings>(signFileSettings);

var authenticationSettings = builder.Configuration.GetSection("Authentication");
builder.Services.Configure<AuthenticationSettings>(authenticationSettings);

var runDiagnosticsSettings = builder.Configuration.GetSection("RunDiagnosticsSettings");
builder.Services.Configure<RunDiagnosticsSettings>(runDiagnosticsSettings);

var strictModeSettingsSection = builder.Configuration.GetSection(WebApplicationExtensions.STRICT_MODE_SETTINGS_SECTION);
builder.Services.Configure<StrictModeSettings>(strictModeSettingsSection);

var isStrictmode = strictModeSettingsSection.GetValue("StrictMode", false);
var activeUrls = new string[] { httpsUrl };
if (!isStrictmode)
{
    activeUrls.Append(httpUrl);
}
builder.WebHost.UseUrls(activeUrls);

builder.Services.AddCors(options =>
        {
            options.AddPolicy(MY_ALLOW_SPECIFICORIGINS, b =>
            {
                b.WithOrigins(activeUrls)
                               .AllowAnyHeader()
                               .AllowAnyMethod();
            });
        });


var servcieProvider = builder.Services.BuildServiceProvider();
var x509Provider = servcieProvider.GetRequiredService<IX509Provider>();

builder.WebHost.UseKestrel(options =>
{
    if (!isStrictmode)
    {
        options.Listen(IPAddress.Any, port);
    }
    options.Listen(IPAddress.Any, httpsPort, listenOptions =>
    {
        listenOptions.UseHttps(x509Provider.GetHttpsCertificate());
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<SwaggerHeader>();
});

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<LogActionFilter>();
    })
    .AddNewtonsoftJson();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(MY_ALLOW_SPECIFICORIGINS);
var isCommunicationLess = builder.Configuration.GetValue<bool?>("CommunicationLess");
if (isCommunicationLess == true)
{
    app.UseMiddleware<CommunicationLessMiddleware>();
}
else
{
    app.UseMiddleware<AuthorizationCheckMiddleware>();
}


app.UseMiddleware<ValidationExceptionHandlerMiddleware>();

app.MapControllers();

app.ValidateAuthenticationSettings();

app.Run();
