
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Sevices;
public class StateMachineListenerService : BackgroundService
{
    private readonly IStateMachineChangedEvent _stateMachineChangedEvent;
    private readonly IServiceProvider _serviceProvider;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private CancellationTokenSource _cts;
    private ITwinHandler? _twinHandler;
    private ITwinReportHandler? _twinReportHandler;
    private IC2DEventSubscriptionSession? _c2DEventSubscriptionSession;
    private IStateMachineHandler? _stateMachineHandlerService;
    private readonly ILoggerHandler _logger;

    public StateMachineListenerService(
        IStateMachineChangedEvent stateMachineChangedEvent,
        IServiceProvider serviceProvider,
        IDeviceClientWrapper deviceClientWrapper,
        ILoggerHandler logger
    )
    {
        _cts = new CancellationTokenSource();
        _stateMachineChangedEvent = stateMachineChangedEvent ?? throw new ArgumentNullException(nameof(stateMachineChangedEvent));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stateMachineChangedEvent.StateChanged += HandleStateChangedEvent;
        using (var scope = _serviceProvider.CreateScope())
        {
            var dpsProvisioningDeviceClientHandler = scope.ServiceProvider.GetService<IDPSProvisioningDeviceClientHandler>();
            ArgumentNullException.ThrowIfNull(dpsProvisioningDeviceClientHandler);
            _stateMachineHandlerService = scope.ServiceProvider.GetService<IStateMachineHandler>();
            ArgumentNullException.ThrowIfNull(_stateMachineHandlerService);
            await dpsProvisioningDeviceClientHandler.InitAuthorizationAsync();
            await _stateMachineHandlerService.InitStateMachineHandlerAsync(stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_stateMachineHandlerService != null)
        {
            var state = _stateMachineHandlerService.GetCurrentDeviceState();

            if(state == DeviceStateType.Provisioning)
            {
                _logger.Info("StopAsync: set device state to Uninitialized");
                await _stateMachineHandlerService.SetStateAsync(DeviceStateType.Uninitialized, _cts.Token);
            }
            else
            {
                if(state != DeviceStateType.Busy && state != DeviceStateType.Uninitialized)
                {
                    _logger.Info("StopAsync: set device state to Busy");
                    if (_twinReportHandler == null)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            _twinReportHandler = scope.ServiceProvider.GetService<ITwinReportHandler>() ?? throw new ArgumentNullException(nameof(_twinReportHandler));
                            await _twinReportHandler.UpdateDeviceStateAfterServiceRestartAsync(state, _cts.Token);
                        }
                    }
                    await _stateMachineHandlerService.SetStateAsync(DeviceStateType.Busy, _cts.Token);
                }
            }
        }
        await base.StopAsync(cancellationToken);
    }

    internal async void HandleStateChangedEvent(object? sender, StateMachineEventArgs e)
    {
        if (_c2DEventSubscriptionSession == null)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                _c2DEventSubscriptionSession = scope.ServiceProvider.GetService<IC2DEventSubscriptionSession>() ?? throw new ArgumentNullException(nameof(_c2DEventSubscriptionSession));
                _twinHandler = scope.ServiceProvider.GetService<ITwinHandler>() ?? throw new ArgumentNullException(nameof(_twinHandler));
            }
        }

        switch (e.NewState)
        {
            case DeviceStateType.Provisioning:
                await SetProvisioningAsync();
                break;
            case DeviceStateType.Ready:
                await SetReadyAsync();
                break;
            case DeviceStateType.Busy:
                await CancelOperationsAsync();
                break;
            default:
                break;
        }
    }

    private async Task SetProvisioningAsync()
    {
        _cts = new CancellationTokenSource();
        if (_c2DEventSubscriptionSession != null)
        {
            await _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(_cts.Token, true);
        }
    }

    private async Task SetReadyAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        if (_c2DEventSubscriptionSession != null && _twinHandler != null)
        {
            // handle twin need to be before recived messages
            await _twinHandler.HandleTwinActionsAsync(_cts.Token);
            await _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(_cts.Token, false);
        }
    }

    private async Task CancelOperationsAsync()
    {
        _cts?.Cancel();
        _twinHandler?.CancelCancellationToken();
        await _deviceClientWrapper.DisposeAsync();
    }
}