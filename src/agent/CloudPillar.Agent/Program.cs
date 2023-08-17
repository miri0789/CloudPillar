using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Factories;
using Shared.Entities.Twin;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IC2DEventHandler, C2DEventHandler>();
        services.AddSingleton<IC2DEventSubscriptionSession, C2DEventSubscriptionSession>();
        services.AddSingleton<IEnvironmentsWrapper, EnvironmentsWrapper>();
        services.AddSingleton<IDeviceClientWrapper, DeviceClientWrapper>();
        services.AddSingleton<IMessageSubscriber, MessageSubscriber>();
        services.AddSingleton<IFileDownloadHandler, FileDownloadHandler>();
        services.AddSingleton<IFileStreamerWrapper, FileStreamerWrapper>();
        services.AddSingleton<ISignatureHandler, SignatureHandler>();
        services.AddSingleton<ID2CMessengerHandler, D2CMessengerHandler>();
        services.AddSingleton<ITwinHandler, TwinHandler>();
        services.AddSingleton<IMessagesFactory, MessagesFactory>();

        
    })
    .Build();


// TODO: execute according to agent design
var signatureHandler = host.Services.GetService<ISignatureHandler>();
signatureHandler.InitPublicKeyAsync();
var c2DEventHandler = host.Services.GetService<IC2DEventHandler>();
c2DEventHandler.CreateSubscribeAsync(new CancellationToken());
var twinHandler = host.Services.GetService<ITwinHandler>();
twinHandler.UpdateDeviceStateAsync(DeviceStateType.Ready);
twinHandler.HandleTwinActionsAsync();

host.Run();
