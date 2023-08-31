using CloudPillar.Agent.API.Entities;
using CloudPillar.Agent.API.Handlers;
using CloudPillar.Agent.API.Utilities;
using CloudPillar.Agent.API.Validators;
using CloudPillar.Agent.API.Wrappers;
using FluentValidation;
using Shared.Entities.Factories;
using Shared.Logger;

const string MY_ALLOW_SPECIFICORIGINS = "AllowLocalhost";
var builder = LoggerHostCreator.Configure("Agent API", WebApplication.CreateBuilder(args));

builder.Services.AddCors(options =>
        {
            options.AddPolicy(MY_ALLOW_SPECIFICORIGINS, b =>
            {
                b.WithOrigins("http://localhost")
                       .AllowAnyHeader()
                       .AllowAnyMethod();
            });
        });

builder.Services.AddScoped<ITwinHandler, TwinHandler>();
builder.Services.AddScoped<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddScoped<IFileDownloadHandler, FileDownloadHandler>();
builder.Services.AddScoped<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<IFileStreamerWrapper, FileStreamerWrapper>();
builder.Services.AddScoped<ID2CMessengerHandler, D2CMessengerHandler>();

builder.Services.AddScoped<IValidator<UpdateReportedProps>, UpdateReportedPropsValidator>();


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

app.UseMiddleware<ValidationExceptionHandlerMiddleware>();

app.UseCors(MY_ALLOW_SPECIFICORIGINS);
app.MapControllers();

app.Run();
