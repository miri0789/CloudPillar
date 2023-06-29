using CloudPillar.Agent.Handlers;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICommonHandler, CommonHandler>();
        services.AddSingleton<IC2DSubscriptionHandler, C2DSubscriptionHandler>();
        services.AddSingleton<IFileDownloadHandler, FileDownloadHandler>();
        services.AddSingleton<IFileStreamerHandler, FileStreamerHandler>();
        services.AddSingleton<ISignatureHandler, SignatureHandler>();
    })
    .Build();

var signatureHandler = host.Services.GetService<ISignatureHandler>();
signatureHandler.InitPublicKeyAsync();
host.Run();
