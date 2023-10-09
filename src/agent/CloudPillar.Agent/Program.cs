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
const string CONFIG_PORT = "Port";
var builder = LoggerHostCreator.Configure("Agent API", WebApplication.CreateBuilder(args));
var port = builder.Configuration.GetValue(CONFIG_PORT, 8099);
var url = $"http://localhost:{port}";
builder.WebHost.UseUrls(url);
builder.Services.AddCors(options =>
        {
            options.AddPolicy(MY_ALLOW_SPECIFICORIGINS, b =>
            {
                b.WithOrigins(url)
                       .AllowAnyHeader()
                       .AllowAnyMethod();
            });
        });

builder.Services.AddSingleton<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddSingleton<IDPSProvisioningDeviceClientHandler, X509DPSProvisioningDeviceClientHandler>();
builder.Services.AddSingleton<IX509CertificateWrapper, X509CertificateWrapper>();
builder.Services.AddScoped<IC2DEventHandler, C2DEventHandler>();
builder.Services.AddScoped<IC2DEventSubscriptionSession, C2DEventSubscriptionSession>();
builder.Services.AddScoped<IMessageSubscriber, MessageSubscriber>();
builder.Services.AddScoped<ISignatureHandler, SignatureHandler>();
builder.Services.AddScoped<IMessageFactory, MessageFactory>();
builder.Services.AddScoped<ICheckSumService, CheckSumService>();
builder.Services.AddScoped<ITwinHandler, TwinHandler>();
builder.Services.AddScoped<IFileDownloadHandler, FileDownloadHandler>();
builder.Services.AddScoped<IFileStreamerWrapper, FileStreamerWrapper>();
builder.Services.AddScoped<ID2CMessengerHandler, D2CMessengerHandler>();
builder.Services.AddScoped<IStreamingFileUploaderHandler, StreamingFileUploaderHandler>();
builder.Services.AddScoped<IBlobStorageFileUploaderHandler, BlobStorageFileUploaderHandler>();
builder.Services.AddScoped<IFileUploaderHandler, FileUploaderHandler>();
builder.Services.AddScoped<IValidator<UpdateReportedProps>, UpdateReportedPropsValidator>();
builder.Services.AddScoped<IRuntimeInformationWrapper, RuntimeInformationWrapper>();
builder.Services.AddScoped<IValidator<TwinDesired>, TwinDesiredValidator>();
builder.Services.AddScoped<IReProvisioningHandler, ReProvisioningHandler>();


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

app.UseMiddleware<AuthorizationCheckMiddleware>();
app.UseMiddleware<ValidationExceptionHandlerMiddleware>();

app.UseCors(MY_ALLOW_SPECIFICORIGINS);
app.MapControllers();


app.Run();