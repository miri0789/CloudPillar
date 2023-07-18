using Microsoft.Azure.Devices.Client;
using CloudPillar.Agent.Interfaces;
using CloudPillar.Agent.Wrappers;

namespace CloudPillar.Agent.Handlers;

public class C2DEventHandler : IC2DEventHandler
{
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly IC2DEventSubscriptionSession _C2DEventSubscriptionSession;

    public C2DEventHandler(IDeviceClientWrapper deviceClientWrapper, IC2DEventSubscriptionSession C2DEventSubscriptionSession)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(C2DEventSubscriptionSession);

        _deviceClientWrapper = deviceClientWrapper;
        _C2DEventSubscriptionSession = C2DEventSubscriptionSession;
    }


    public async Task CreateSubscribe(CancellationToken cancellationToken, string connectionString)
    {
        DeviceClient deviceClient = _deviceClientWrapper.CreateDeviceClient(connectionString);
        Console.WriteLine("Subscribing to C2D messages...");

        Task.Run(() => _C2DEventSubscriptionSession.ReceiveC2DMessagesAsync(deviceClient, cancellationToken), cancellationToken);
    }

}