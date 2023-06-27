using CloudPillar.Agent.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IFileStreamerHandler, FileStreamerHandler>();
        services.AddSingleton<ISignatureHandler, SignatureHandler>();
    })
    .Build();

var signatureHandler = host.Services.GetService<ISignatureHandler>();
signatureHandler.InitPublicKeyAsync();
host.Run();
