using System.Net;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Sevices;
using CloudPillar.Agent.Utilities;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Factories;
using Shared.Entities.Services;
using System.Runtime.InteropServices;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers.Interfaces;
using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Sevices.Interfaces;
using Shared.Entities.Twin;
using CloudPillar.Agent.Utilities.Interfaces;

bool runAsService = args.FirstOrDefault() == "--winsrv";
Environment.CurrentDirectory = Directory.GetCurrentDirectory();
if (!runAsService && args.FirstOrDefault() != null)
{
    Environment.CurrentDirectory = args.FirstOrDefault();
}

const string MY_ALLOW_SPECIFICORIGINS = "AllowLocalhost";
var builder = LoggerHostCreator.Configure("Agent API", WebApplication.CreateBuilder(args));
var port = builder.Configuration.GetValue(Constants.CONFIG_PORT, Constants.HTTP_DEFAULT_PORT);
var httpsPort = builder.Configuration.GetValue(Constants.HTTPS_CONFIG_PORT, Constants.HTTPS_DEFAULT_PORT);
var httpUrl = $"http://localhost:{port}";
var httpsUrl = $"https://localhost:{httpsPort}";

var validPorts = new List<int>();
CheckValidPorts();

var serviceName = string.IsNullOrWhiteSpace(builder.Configuration.GetValue("AgentServiceName", Constants.AGENT_SERVICE_DEFAULT_NAME)) ? Constants.AGENT_SERVICE_DEFAULT_NAME : builder.Configuration.GetValue("AgentServiceName", Constants.AGENT_SERVICE_DEFAULT_NAME);

var authenticationSettings = builder.Configuration.GetSection("Authentication");
builder.Services.Configure<AuthenticationSettings>(options =>
        {
            authenticationSettings.Bind(options);

            var storeLocation = authenticationSettings.GetValue<string?>("StoreLocation", null);
            var userName = authenticationSettings.GetValue("UserName", "");

            if (storeLocation != null)
            {
                options.StoreLocation = (StoreLocation)Enum.Parse(typeof(StoreLocation), storeLocation);
            }
            else
            {
                options.StoreLocation = string.IsNullOrWhiteSpace(userName) ? StoreLocation.LocalMachine : StoreLocation.CurrentUser;
            }
        });

if (runAsService && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    var serviceDescription = builder.Configuration.GetValue("ServiceDescription", Constants.AGENT_SERVICE_DEFAULT_DESCRIPTION);
    var password = args.Length > 1 ? args[1] : null;
    builder.Services.AddScoped<IWindowsServiceUtils, WindowsServiceUtils>();
    builder.Services.AddScoped<IWindowsServiceHandler, WindowsServiceHandler>();
    var windowsServiceHandler = builder.Services.BuildServiceProvider().GetRequiredService<IWindowsServiceHandler>();
    windowsServiceHandler?.InstallWindowsService(serviceName, Environment.CurrentDirectory, string.IsNullOrWhiteSpace(serviceDescription) ? Constants.AGENT_SERVICE_DEFAULT_DESCRIPTION : serviceDescription, password);
    Environment.Exit(0);
}

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = serviceName;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<StateMachineListenerService>();
builder.Services.AddSingleton<IStateMachineChangedEvent, StateMachineChangedEvent>();
builder.Services.AddSingleton<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddSingleton<ICheckExceptionResult, CheckExceptionResult>();
builder.Services.AddSingleton<IConfigurationWrapper, ConfigurationWrapper>();
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
builder.Services.AddScoped<ITwinReportHandler, TwinReportHandler>();
builder.Services.AddScoped<ITwinHandler, TwinHandler>();
builder.Services.AddScoped<IFileDownloadHandler, FileDownloadHandler>();
builder.Services.AddScoped<ICloudBlockBlobWrapper, CloudBlockBlobWrapper>();
builder.Services.AddScoped<IFileStreamerWrapper, FileStreamerWrapper>();
builder.Services.AddScoped<ID2CMessengerHandler, D2CMessengerHandler>();
builder.Services.AddScoped<IStreamingFileUploaderHandler, StreamingFileUploaderHandler>();
builder.Services.AddScoped<IBlobStorageFileUploaderHandler, BlobStorageFileUploaderHandler>();
builder.Services.AddScoped<IFileUploaderHandler, FileUploaderHandler>();
builder.Services.AddScoped<IRuntimeInformationWrapper, RuntimeInformationWrapper>();
builder.Services.AddScoped<ISymmetricKeyWrapper, SymmetricKeyWrapper>();
builder.Services.AddScoped<IReprovisioningHandler, ReprovisioningHandler>();
builder.Services.AddScoped<ISHA256Wrapper, SHA256Wrapper>();
builder.Services.AddScoped<IProvisioningServiceClientWrapper, ProvisioningServiceClientWrapper>();
builder.Services.AddScoped<IProvisioningDeviceClientWrapper, ProvisioningDeviceClientWrapper>();
builder.Services.AddScoped<IMatcherWrapper, MatcherWrapper>();
builder.Services.AddScoped<IGuidWrapper, GuidWrapper>();
builder.Services.AddScoped<IRequestWrapper, RequestWrapper>();
builder.Services.AddScoped<IStateMachineHandler, StateMachineHandler>();
builder.Services.AddScoped<IRunDiagnosticsHandler, RunDiagnosticsHandler>();
builder.Services.AddScoped<IX509Provider, X509Provider>();
builder.Services.AddScoped<IAsymmetricAlgorithmWrapper, AsymmetricAlgorithmWrapper>();
builder.Services.AddScoped<IPeriodicUploaderHandler, PeriodicUploaderHandler>();
builder.Services.AddScoped<IServerIdentityHandler, ServerIdentityHandler>();
builder.Services.AddScoped<IProvisioningService, ProvisioningService>();
builder.Services.AddScoped<IHttpContextWrapper, HttpContextWrapper>();

var appSettings = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettings);

var DownloadSettings = builder.Configuration.GetSection("DownloadSettings");
builder.Services.Configure<DownloadSettings>(DownloadSettings);

var runDiagnosticsSettings = builder.Configuration.GetSection("RunDiagnosticsSettings");
builder.Services.Configure<RunDiagnosticsSettings>(runDiagnosticsSettings);

var uploadCompleteRetrySettings = builder.Configuration.GetSection("UploadCompleteRetrySettings");
builder.Services.Configure<UploadCompleteRetrySettings>(uploadCompleteRetrySettings);

var strictModeSettingsSection = builder.Configuration.GetSection(WebApplicationExtensions.STRICT_MODE_SETTINGS_SECTION);
builder.Services.Configure<StrictModeSettings>(strictModeSettingsSection);

var isStrictmode = strictModeSettingsSection.GetValue("StrictMode", false);
var isAllowHTTPAPI = builder.Configuration.GetValue<bool>(Constants.ALLOW_HTTP_API, false);
var activeUrls = new string[] { httpsUrl };
if (!isStrictmode || isAllowHTTPAPI)
{
    activeUrls = activeUrls.Append(httpUrl).ToArray();
}
activeUrls = activeUrls.Where(x =>
{
    var port = x.Split(":").Last();
    if (validPorts.Contains(int.Parse(port)))
    {
        return true;
    }
    return false;
}).ToArray();
if (!activeUrls.Any())
{
    ExitApplication("Invalid active Urls");
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
    if ((!isStrictmode || isAllowHTTPAPI) && validPorts.Contains(port))
    {
        options.Listen(IPAddress.Loopback, port);
    }
    if (validPorts.Contains(httpsPort))
    {
        options.Listen(IPAddress.Loopback, httpsPort, listenOptions =>
     {
         listenOptions.UseHttps(x509Provider.GetHttpsCertificate());
     });
    }
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

app.UseExceptionHandler(new ExceptionHandlerOptions()
{
    AllowStatusCode404Response = true,
    ExceptionHandlingPath = "/error"
});
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

app.MapControllers();

app.ValidateAuthenticationSettings();

app.Run();


void CheckValidPorts()
{
    if (IsValidPort(port))
    {
        validPorts.Add(port);
    }
    if (IsValidPort(httpsPort))
    {
        validPorts.Add(httpsPort);
    }
    if (!validPorts.Any())
    {
        ExitApplication("Invalid HTTP and HTTPS port. Please provide valid port numbers.");
    }
}

// Function to check if a port is valid
bool IsValidPort(int port)
{
    return port > 0 && port <= 65535; // Ports must be in the range 1-65535    
}

void ExitApplication(string message)
{
    Console.WriteLine(message);
    Environment.Exit(1);
}