using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddScoped<IC2DEventHandler, C2DEventHandler>();
        services.AddScoped<IC2DEventSubscriptionSession, C2DEventSubscriptionSession>();
        services.AddSingleton<IFileDownloadHandler, FileDownloadHandler>();
        services.AddSingleton<IFileStreamerWrapper, FileStreamerWrapper>();
        services.AddSingleton<ISignatureHandler, SignatureHandler>();
        services.AddSingleton<ID2CEventHandler, D2CEventHandler>();
        services.AddSingleton<IDeviceClientWrapper, DeviceClientWrapper>();
        services.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
        services.AddSingleton<IMessageSubscriber, MessageSubscriber>();

        
    })
    .Build();

var signatureHandler = host.Services.GetService<ISignatureHandler>();
signatureHandler.InitPublicKeyAsync();
host.Run();
