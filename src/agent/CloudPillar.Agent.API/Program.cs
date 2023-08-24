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
// builder.Services.AddSingleton<IC2DEventHandler, C2DEventHandler>();
// builder.Services.AddSingleton<IDeviceClientWrapper, DeviceClientWrapper>();
// builder.Services.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
// builder.Services.AddSingleton<IC2DEventSubscriptionSession, C2DEventSubscriptionSession>();
// builder.Services.AddSingleton<IMessageSubscriber, MessageSubscriber>();
// builder.Services.AddSingleton<IMessagesFactory, MessagesFactory>();
// builder.Services.AddSingleton<ITwinHandler, TwinHandler>();
// builder.Services.AddSingleton<IFileDownloadHandler, FileDownloadHandler>();
// builder.Services.AddSingleton<IFileStreamerWrapper, FileStreamerWrapper>();
// builder.Services.AddSingleton<ID2CMessengerHandler, D2CMessengerHandler>();



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
