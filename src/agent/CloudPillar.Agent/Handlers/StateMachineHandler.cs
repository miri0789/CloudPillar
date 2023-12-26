using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;

namespace CloudPillar.Agent.Handlers
{
    public class StateMachineHandler : IStateMachineHandler
    {
        private readonly ITwinHandler _twinHandler;
        private readonly ITwinReportHandler _twinReportHandler;
        private readonly IStateMachineChangedEvent _stateMachineChangedEvent;
        private readonly ILoggerHandler _logger;
        private static DeviceStateType _currentDeviceState = DeviceStateType.Uninitialized;


        public StateMachineHandler(
            ITwinHandler twinHandler,
            IStateMachineChangedEvent stateMachineChangedEvent,
            ILoggerHandler logger,
            ITwinReportHandler twinReportHandler
         )
        {
            _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
            _twinReportHandler = twinReportHandler ?? throw new ArgumentNullException(nameof(twinReportHandler));
            _stateMachineChangedEvent = stateMachineChangedEvent ?? throw new ArgumentNullException(nameof(stateMachineChangedEvent));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task InitStateMachineHandlerAsync(CancellationToken cancellationToken)
        {
            var state = await GetInitStateAsync();
            _logger.Info($"InitStateMachineHandlerAsync: init device state: {state}");
            _currentDeviceState = state;
            if(state != await GetStateAsync())
            {
                await SetStateAsync(state, cancellationToken);
                await _twinReportHandler.UpdateDeviceStateAfterServiceRestartAsync(null, cancellationToken);
            }
            else
            {
                await HandleStateActionAsync(state);
            }
        }

        public async Task SetStateAsync(DeviceStateType state, CancellationToken cancellationToken)
        {

            var currentState = await GetStateAsync();
            if (currentState != state || state == DeviceStateType.Provisioning)
            {
                _currentDeviceState = state;
                await _twinReportHandler.UpdateDeviceStateAsync(state, cancellationToken);
                await HandleStateActionAsync(state);
                _logger.Info($"Set device state: {state}");
            }

        }

        private async Task HandleStateActionAsync(DeviceStateType state)
        {
            if (state == DeviceStateType.Busy)
            {
                await _twinHandler.SaveLastTwinAsync();
            }
            _logger.Info($"Handle state action, state: {state}");
            _stateMachineChangedEvent.SetStateChanged(new StateMachineEventArgs(state));
        }

        public async Task<DeviceStateType> GetStateAsync()
        {
            var state = await _twinReportHandler.GetDeviceStateAsync() ?? GetCurrentDeviceState();
            return state;
        }

        public DeviceStateType GetCurrentDeviceState()
        {
            return _currentDeviceState;
        }

        public async Task<DeviceStateType> GetInitStateAsync()
        {
            var state = await _twinReportHandler.GetDeviceStateAfterServiceRestartAsync() ?? await _twinReportHandler.GetDeviceStateAsync() ?? GetCurrentDeviceState();
            return state;
        }

    }
}