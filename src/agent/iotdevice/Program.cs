using iotdevice.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddScoped<IFileStreamerService, FileStreamerService>();
    })
    .Build();

host.Run();
