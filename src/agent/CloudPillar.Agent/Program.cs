using System.Net;
using System.Runtime.InteropServices;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Utilities;
using CloudPillar.Agent.Validators;
using CloudPillar.Agent.Wrappers;
using FluentValidation;
using Shared.Entities.Factories;
using Shared.Entities.Services;
using Shared.Entities.Twin;
using Shared.Logger;

const string MY_ALLOW_SPECIFICORIGINS = "AllowLocalhost";
var builder = LoggerHostCreator.Configure("Agent API", WebApplication.CreateBuilder(args));
var port = builder.Configuration.GetValue(Constants.CONFIG_PORT, Constants.HTTP_DEFAULT_PORT);
var sslPort = builder.Configuration.GetValue(Constants.CONFIG_PORT, Constants.HTTPS_DEFAULT_PORT);
var url = $"http://localhost:{port}";
var sslUrl = $"https://localhost:{sslPort}";

builder.WebHost.UseUrls(url, sslUrl);

builder.Services.AddCors(options =>
        {
            options.AddPolicy(MY_ALLOW_SPECIFICORIGINS, b =>
            {
                b.WithOrigins(url, sslUrl)
                               .AllowAnyHeader()
                               .AllowAnyMethod();
            });
        });

builder.Services.AddScoped<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddScoped<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<IDPSProvisioningDeviceClientHandler, X509DPSProvisioningDeviceClientHandler>();
builder.Services.AddScoped<IX509CertificateWrapper, X509CertificateWrapper>();
builder.Services.AddSingleton<IStrictModeHandler, StrictModeHandler>();
builder.Services.AddScoped<ISymmetricKeyProvisioningHandler, SymmetricKeyProvisioningHandler>();
builder.Services.AddScoped<IC2DEventHandler, C2DEventHandler>();
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
builder.Services.AddSingleton<IStateMachineTokenHandler, StateMachineTokenHandler>();

var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettingsSection);

builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<SwaggerHeader>();
});

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<LogActionFilter>();
    });
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors(MY_ALLOW_SPECIFICORIGINS);
app.UseCors(MY_ALLOW_SPECIFICORIGINS);

app.UseMiddleware<AuthorizationCheckMiddleware>();
app.UseMiddleware<ValidationExceptionHandlerMiddleware>();

app.MapControllers();
var strictModeHandler = app.Services.GetService<IStrictModeHandler>();
strictModeHandler.CheckAuthentucationMethodValue();

using (var scope = app.Services.CreateScope())
{
    var dpsProvisioningDeviceClientHandler = scope.ServiceProvider.GetService<IDPSProvisioningDeviceClientHandler>();
    await dpsProvisioningDeviceClientHandler.InitAuthorizationAsync();

    var StateMachineHandlerService = scope.ServiceProvider.GetService<IStateMachineHandler>();
    StateMachineHandlerService.InitStateMachineHandlerAsync();
}

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





