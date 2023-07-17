using CloudPillar.Agent.Factories;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Interfaces;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IC2DEventHandler, C2DEventHandler>();
        services.AddSingleton<IC2DEventSubscriptionSession, C2DEventSubscriptionSession>();
        services.AddSingleton<IFileDownloadHandler, FileDownloadHandler>();
        services.AddSingleton<IFileStreamerFactory, FileStreamerFactory>();
        services.AddSingleton<ISignatureHandler, SignatureHandler>();
        services.AddSingleton<ID2CEventHandler, D2CEventHandler>();
        services.AddSingleton<IDeviceClientWrapper, DeviceClientWrapper>();
        services.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
    })
    .Build();

var signatureHandler = host.Services.GetService<ISignatureHandler>();
signatureHandler.InitPublicKeyAsync();
host.Run();
