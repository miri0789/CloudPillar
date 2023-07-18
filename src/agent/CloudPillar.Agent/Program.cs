using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IC2DSubscriptionHandler, C2DSubscriptionHandler>();
        services.AddSingleton<IFileDownloadHandler, FileDownloadHandler>();
        services.AddSingleton<IFileStreamerWrapper, FileStreamerWrapper>();
        services.AddSingleton<ISignatureHandler, SignatureHandler>();
        services.AddSingleton<ID2CEventHandler, D2CEventHandler>();
        services.AddSingleton<IDeviceClientWrapper, DeviceClientWrapper>();
        services.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
    })
    .Build();

var signatureHandler = host.Services.GetService<ISignatureHandler>();
signatureHandler.InitPublicKeyAsync();
host.Run();
