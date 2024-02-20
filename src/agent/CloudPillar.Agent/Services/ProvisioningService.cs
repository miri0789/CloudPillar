using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Sevices.Interfaces;
using CloudPillar.Agent.Wrappers.Interfaces;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Sevices;

public class ProvisioningService : IProvisioningService
{
    public readonly IStateMachineHandler _stateMachineHandler;
    private readonly IServerIdentityHandler _serverIdentityHandler;
    private readonly IRequestWrapper _requestWrapper;
    private readonly IReprovisioningHandler _reprovisioningHandler;
    private readonly ISymmetricKeyProvisioningHandler _symmetricKeyProvisioningHandler;
    private readonly IStateMachineChangedEvent _stateMachineChangedEvent;
    private readonly ITwinReportHandler _twinReportHandler;
    private readonly ID2CMessengerHandler d2CMessengerHandler;
    public ProvisioningService(IStateMachineHandler stateMachineHandler, IServerIdentityHandler serverIdentityHandler, IRequestWrapper requestWrapper,
        IReprovisioningHandler reprovisioningHandler, ISymmetricKeyProvisioningHandler symmetricKeyProvisioningHandler, IStateMachineChangedEvent stateMachineChangedEvent,
        ITwinReportHandler twinReportHandler, ID2CMessengerHandler d2CMessengerHandler)
    {
        _stateMachineHandler = stateMachineHandler ?? throw new ArgumentNullException(nameof(stateMachineHandler));
        _serverIdentityHandler = serverIdentityHandler ?? throw new ArgumentNullException(nameof(serverIdentityHandler));
        _requestWrapper = requestWrapper ?? throw new ArgumentNullException(nameof(requestWrapper));
        _reprovisioningHandler = reprovisioningHandler ?? throw new ArgumentNullException(nameof(reprovisioningHandler));
        _symmetricKeyProvisioningHandler = symmetricKeyProvisioningHandler ?? throw new ArgumentNullException(nameof(symmetricKeyProvisioningHandler));
        _stateMachineChangedEvent = stateMachineChangedEvent ?? throw new ArgumentNullException(nameof(stateMachineChangedEvent));
        _twinReportHandler = twinReportHandler ?? throw new ArgumentNullException(nameof(twinReportHandler));
        this.d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
    }

    public async Task ProvisinigSymetricKeyAsync(CancellationToken cancellationToken)
    {
        await _stateMachineHandler.SetStateAsync(DeviceStateType.Uninitialized, cancellationToken);
        await _serverIdentityHandler.RemoveNonDefaultCertificatesAsync(SharedConstants.PKI_FOLDER_PATH);
        _stateMachineChangedEvent.SetStateChanged(new StateMachineEventArgs(DeviceStateType.Busy));
        _reprovisioningHandler.RemoveX509CertificatesFromStore();
        //don't need to explicitly check if the header exists; it's already verified in the middleware.
        var deviceId = _requestWrapper.GetHeaderValue(Constants.X_DEVICE_ID);
        var secretKey = _requestWrapper.GetHeaderValue(Constants.X_SECRET_KEY);
        if (!await _symmetricKeyProvisioningHandler.ProvisioningAsync(deviceId, cancellationToken))
        {
            throw new Exception("Failed to provision symmetric key");
        }
        if (await _symmetricKeyProvisioningHandler.IsNewDeviceAsync(cancellationToken))
        {
            await _stateMachineHandler.SetStateAsync(DeviceStateType.Provisioning, cancellationToken, true);
            await _twinReportHandler.InitReportDeviceParamsAsync(cancellationToken);
            await _twinReportHandler.UpdateDeviceSecretKeyAsync(secretKey, cancellationToken);
        }
        else
        {
            await d2CMessengerHandler.SendRemoveDeviceEvent(cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            await ProvisinigSymetricKeyAsync(cancellationToken);
        }
    }
}