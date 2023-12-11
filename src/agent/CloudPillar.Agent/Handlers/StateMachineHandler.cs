
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;

namespace CloudPillar.Agent.Handlers
{
    public class StateMachineHandler : IStateMachineHandler
    {
        private readonly ITwinHandler _twinHandler;
        private readonly IStateMachineChangedEvent _stateMachineChangedEvent;
        private readonly ILoggerHandler _logger;
        private static DeviceStateType _currentDeviceState = DeviceStateType.Uninitialized;


        public StateMachineHandler(
            ITwinHandler twinHandler,
           IStateMachineChangedEvent stateMachineChangedEvent,
         ILoggerHandler logger
         )
        {
            _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
            _stateMachineChangedEvent = stateMachineChangedEvent ?? throw new ArgumentNullException(nameof(stateMachineChangedEvent));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task InitStateMachineHandlerAsync()
        {
            var state = await GetStateAsync();
            _logger.Info($"InitStateMachineHandlerAsync: init device state: {state}");
            _currentDeviceState = state;
            await HandleStateActionAsync(state);
        }

        public async Task SetStateAsync(DeviceStateType state, CancellationToken cancellationToken)
        {

            var currentState = await GetStateAsync();
            if (currentState != state || state == DeviceStateType.Provisioning)
            {
                _currentDeviceState = state;
                await _twinHandler.UpdateDeviceStateAsync(state, cancellationToken);
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
            var state = await _twinHandler.GetDeviceStateAsync() ?? GetCurrentDeviceState();
            return state;
        }

        public DeviceStateType GetCurrentDeviceState()
        {
            return _currentDeviceState;
        }

    }
}