using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Interfaces;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICommonHandler, CommonHandler>();
        services.AddSingleton<IC2DSubscriptionHandler, C2DSubscriptionHandler>();
        services.AddSingleton<IFileDownloadHandler, FileDownloadHandler>();
        services.AddSingleton<IFileStreamerFactory, FileStreamerFactory>();
        services.AddSingleton<ISignatureHandler, SignatureHandler>();
    })
    .Build();

var signatureHandler = host.Services.GetService<ISignatureHandler>();
signatureHandler.InitPublicKeyAsync();
host.Run();
