using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Sevices.interfaces;
using CloudPillar.Agent.Wrappers;
using Newtonsoft.Json;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Sevices;

public class InitiateProvisioningService : IInitiateProvisioningService
{
    private readonly IServerIdentityHandler _serverIdentityHandler;
    private readonly IStateMachineHandler _stateMachineHandler;
    private readonly ITwinHandler _twinHandler;
    private readonly IStateMachineChangedEvent _stateMachineChangedEvent;
    private readonly ISymmetricKeyProvisioningHandler _symmetricKeyProvisioningHandler;
    private readonly IRemoveX509Certificates _removeX509Certificates;
    private readonly ITwinReportHandler _twinReportHandler;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;

    public InitiateProvisioningService(
        IServerIdentityHandler serverIdentityHandler,
        IStateMachineHandler stateMachineHandler,
        ITwinHandler twinHandler,
        IStateMachineChangedEvent stateMachineChangedEvent,
        ISymmetricKeyProvisioningHandler symmetricKeyProvisioningHandler,
        IRemoveX509Certificates removeX509Certificates,
        ITwinReportHandler twinReportHandler,
        IDeviceClientWrapper deviceClient,
        ID2CMessengerHandler d2CMessengerHandler
    )
    {
        _serverIdentityHandler = serverIdentityHandler ?? throw new ArgumentNullException(nameof(serverIdentityHandler));
        _stateMachineHandler = stateMachineHandler ?? throw new ArgumentNullException(nameof(StateMachineHandler));
        _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
        _stateMachineChangedEvent = stateMachineChangedEvent ?? throw new ArgumentNullException(nameof(stateMachineChangedEvent));
        _symmetricKeyProvisioningHandler = symmetricKeyProvisioningHandler ?? throw new ArgumentNullException(nameof(symmetricKeyProvisioningHandler));
        _removeX509Certificates = removeX509Certificates ?? throw new ArgumentNullException(nameof(removeX509Certificates));
        _twinReportHandler = twinReportHandler ?? throw new ArgumentNullException(nameof(twinReportHandler));
        _deviceClientWrapper = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
    }

    public async Task<string> InitiateProvisioningAsync(string deviceId, string secretKey, CancellationToken cancellationToken)
    {
        await _serverIdentityHandler.HandleKnownIdentitiesFromCertificatesAsync(cancellationToken);
        await _stateMachineHandler.SetStateAsync(DeviceStateType.Uninitialized, cancellationToken);
        var isDeviceExist = await ProvisinigSymetricKeyAsync(deviceId, secretKey, cancellationToken);
        return isDeviceExist ? null : await _twinHandler.GetTwinJsonAsync();
    }

    private async Task<bool> ProvisinigSymetricKeyAsync(string deviceId, string secretKey, CancellationToken cancellationToken)
    {
        //don't need to explicitly check if the header exists; it's already verified in the middleware.
        _stateMachineChangedEvent.SetStateChanged(new StateMachineEventArgs(DeviceStateType.Busy));
        await _symmetricKeyProvisioningHandler.ProvisioningAsync(deviceId, cancellationToken);
        _removeX509Certificates.RemoveX509CertificatesFromStore();
        var isDeviceExist = await IsDeviceExistAsync(cancellationToken);
        if (isDeviceExist)
        {
            await _d2CMessengerHandler.SendRemoveDeviceEvent(deviceId, cancellationToken);
        }
        else
        {
            await _stateMachineHandler.SetStateAsync(DeviceStateType.Provisioning, cancellationToken, true);
            await _twinReportHandler.InitReportDeviceParamsAsync(cancellationToken);
            await _twinReportHandler.UpdateDeviceSecretKeyAsync(secretKey, cancellationToken);
        }
        return isDeviceExist;
    }

    private async Task<bool> IsDeviceExistAsync(CancellationToken cancellationToken)
    {
        var twin = await _deviceClientWrapper.GetTwinAsync(cancellationToken);
        var reported = JsonConvert.DeserializeObject<TwinReported>(twin.Properties.Reported.ToJson());
        return reported?.SupportedShells is not null && reported?.AgentPlatform is not null;
    }
}