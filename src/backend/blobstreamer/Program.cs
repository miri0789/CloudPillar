using blobstreamer.Services;
using blobstreamer.Contracts;
using System.Reflection;

var informationalVersion = Assembly.GetEntryAssembly()?
                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                               .InformationalVersion;
Console.WriteLine($"Informational Version: {informationalVersion ?? "Unknown"}");

var builder = WebApplication.CreateBuilder(args);

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
