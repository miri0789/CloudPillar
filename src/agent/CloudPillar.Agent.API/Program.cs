using CloudPillar.Agent.API.Handlers;
using CloudPillar.Agent.API.Utilities;
using CloudPillar.Agent.API.Wrappers;
using Shared.Entities.Factories;
using Shared.Logger;

string myAllowSpecificOrigins = "AllowLocalhost";
var builder = LoggerHostCreator.Configure("Agent API", WebApplication.CreateBuilder(args));

builder.Services.AddCors(options =>
        {
            options.AddPolicy(myAllowSpecificOrigins, b =>
            {
                b.WithOrigins("http://localhost")
                       .AllowAnyHeader()
                       .AllowAnyMethod();
            });
        });
// builder.Services.AddScoped<IC2DEventHandler, C2DEventHandler>();
// builder.Services.AddScoped<IC2DEventSubscriptionSession, C2DEventSubscriptionSession>();
// builder.Services.AddScoped<IMessageSubscriber, MessageSubscriber>();
// builder.Services.AddScoped<IMessagesFactory, MessagesFactory>();
builder.Services.AddScoped<ITwinHandler, TwinHandler>();
builder.Services.AddScoped<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddScoped<IFileDownloadHandler, FileDownloadHandler>();
builder.Services.AddScoped<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<IFileStreamerWrapper, FileStreamerWrapper>();
builder.Services.AddScoped<ID2CMessengerHandler, D2CMessengerHandler>();



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

app.UseCors(myAllowSpecificOrigins);
app.UseAuthorization();
app.MapControllers();

app.Run();
