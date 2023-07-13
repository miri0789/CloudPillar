using Backend.BlobStreamer.Services;
using Backend.BlobStreamer.Interfaces;
using System.Reflection;

var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;
Console.WriteLine($"Informational Version: {informationalVersion ?? "Unknown"}");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICloudStorageWrapper, CloudStorageWrapper>();
builder.Services.AddSingleton<IDeviceClientWrapper, DeviceClientWrapper>();
builder.Services.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
builder.Services.AddScoped<IBlobService, BlobService>();

builder.Services.AddControllers();

builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
