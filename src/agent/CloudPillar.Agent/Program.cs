using System.Net;
using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Sevices;
using CloudPillar.Agent.Utilities;
using CloudPillar.Agent.Validators;
using CloudPillar.Agent.Wrappers;
using FluentValidation;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Azure;
using Shared.Entities.Factories;
using Shared.Entities.Services;
using Shared.Entities.Twin;
using Shared.Logger;

const string MY_ALLOW_SPECIFICORIGINS = "AllowLocalhost";
var builder = LoggerHostCreator.Configure("Agent API", WebApplication.CreateBuilder(args));
var port = builder.Configuration.GetValue(Constants.CONFIG_PORT, Constants.HTTP_DEFAULT_PORT);
var httpsPort = builder.Configuration.GetValue(Constants.HTTPS_CONFIG_PORT, Constants.HTTPS_DEFAULT_PORT);
var httpUrl = $"http://localhost:{port}";
var httpsUrl = $"https://localhost:{httpsPort}";

builder.WebHost.UseUrls(httpUrl, httpsUrl);



builder.Services.AddCors(options =>
        {
            options.AddPolicy(MY_ALLOW_SPECIFICORIGINS, b =>
            {
                b.WithOrigins(httpUrl, httpsUrl)
                               .AllowAnyHeader()
                               .AllowAnyMethod();
            });
        });

builder.Services.AddHostedService<StateMachineListenerService>();
builder.Services.AddSingleton<IStateMachineChangedEvent, StateMachineChangedEvent>();
builder.Services.AddSingleton<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<IDPSProvisioningDeviceClientHandler, X509DPSProvisioningDeviceClientHandler>();
builder.Services.AddScoped<IX509CertificateWrapper, X509CertificateWrapper>();
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
builder.Services.AddScoped<IStateMachineHandler, StateMachineHandler>();
builder.Services.AddScoped<IRunDiagnosticsHandler, RunDiagnosticsHandler>();
builder.Services.AddScoped<IX509Provider, X509Provider>();

var strictModeSettingsSection = builder.Configuration.GetSection(WebApplicationExtensions.STRICT_MODE_SETTINGS_SECTION);
builder.Services.Configure<StrictModeSettings>(strictModeSettingsSection);

var authenticationSettings = builder.Configuration.GetSection("Authentication");
builder.Services.Configure<AuthenticationSettings>(authenticationSettings);

var runDiagnosticsSettings = builder.Configuration.GetSection("RunDiagnosticsSettings");
builder.Services.Configure<RunDiagnosticsSettings>(runDiagnosticsSettings);

var servcieProvider = builder.Services.BuildServiceProvider();
var x509Provider = servcieProvider.GetRequiredService<IX509Provider>();
builder.WebHost.UseKestrel(options =>
{
    options.Listen(IPAddress.Any, port);
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
bool runAsService = args != null && args.Length > 0 && args[0] == "--winsrv";

if (runAsService && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Services.AddHostedService<Worker>();
    builder.Services.AddWindowsService();
    InstallWindowsService();

}
// else
// {

// }

var app = builder.Build();



app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(MY_ALLOW_SPECIFICORIGINS);

app.UseMiddleware<AuthorizationCheckMiddleware>();
app.UseMiddleware<ValidationExceptionHandlerMiddleware>();

app.MapControllers();

app.ValidateAuthenticationSettings();

app.Run();
    
public partial class Program
    {
        // P/Invoke declarations
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr OpenSCManager(string machineName, string databaseName, uint dwAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern IntPtr CreateService(IntPtr hSCManager, string lpServiceName, string lpDisplayName, uint dwDesiredAccess, uint dwServiceType, uint dwStartType, uint dwErrorControl, string lpBinaryPathName, string lpLoadOrderGroup, IntPtr lpdwTagId, string lpDependencies, string lpServiceStartName, string lpPassword);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr hSCObject);

        // Constants
        private const uint SC_MANAGER_CREATE_SERVICE = 0x0002;
        private const uint SERVICE_WIN32_SHARE_PROCESS = 0x00000020;
        private const uint SERVICE_AUTO_START = 0x00000002;
        private const uint SERVICE_ERROR_NORMAL = 0x00000001;

        public static void InstallWindowsService()
    {
        IntPtr scm = OpenSCManager(null, null, SC_MANAGER_CREATE_SERVICE);
        if (scm == IntPtr.Zero)
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        //string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + " --winsrv";
        string exePath = AppDomain.CurrentDomain.BaseDirectory + "CloudPillar.Agent.exe" + " --winsrv" ;

        IntPtr svc = CreateService(scm, "CP_Agent_Service1", "Cloud Pillar Agent Service1", SC_MANAGER_CREATE_SERVICE, SERVICE_WIN32_SHARE_PROCESS, SERVICE_AUTO_START, SERVICE_ERROR_NORMAL, exePath, null, IntPtr.Zero, null, null, null);

        if (svc == IntPtr.Zero)
        {
            CloseServiceHandle(scm);
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }

        CloseServiceHandle(svc);
        CloseServiceHandle(scm);
    }
}

public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
        _logger.LogInformation("Worker is starting.");

        stoppingToken.Register(() => _logger.LogInformation("Worker is stopping."));

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker is doing background work.");

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("Worker has stopped.");

        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                 _logger.LogInformation("Worker is starting at: {time}", DateTimeOffset.Now);

            // Your setup logic here

            return base.StartAsync(cancellationToken);
            }
            catch (System.Exception)
            {
                
                throw;
            }
           
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker is stopping.");

            // Your graceful shutdown logic here

            return base.StopAsync(cancellationToken);
        }

        // public override async Task PauseAsync(CancellationToken cancellationToken)
        // {
        //     _logger.LogInformation("Worker is pausing.");

        //     // Your graceful pause logic here

        //     await base.PauseAsync(cancellationToken);
        // }

        // public override async Task ResumeAsync(CancellationToken cancellationToken)
        // {
        //     _logger.LogInformation("Worker is resuming.");

        //     // Your graceful resume logic here

        //     await base.ResumeAsync(cancellationToken);
        // }
    }





