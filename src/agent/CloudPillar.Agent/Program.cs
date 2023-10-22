using System.Net;
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
builder.Services.AddScoped<IStrictModeHandler, StrictModeHandler>();
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
builder.Services.AddScoped<IStateMachine, StateMachine>();

var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettingsSection);

builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
    options.HttpsPort = sslPort;
});
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<SwaggerHeader>();
});

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<LogActionFilter>();
    });
builder.Services.AddSwaggerGen();

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

app.Run();