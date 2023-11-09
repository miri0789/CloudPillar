
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers
{
    public class StateMachineHandler : IStateMachineHandler
    {
        private readonly ITwinHandler _twinHandler;
        private readonly IStateMachineChangedEvent _stateMachineChangedEvent;
        private readonly ILoggerHandler _logger;
        private static DeviceStateType currentDeviceState = DeviceStateType.Uninitialized;


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
            currentDeviceState = state;
            HandleStateAction(state);
        }

        public async Task SetStateAsync(DeviceStateType state)
        {

            var currentState = await GetStateAsync();
            if (currentState != state)
            {
                currentDeviceState = state;
                await _twinHandler.UpdateDeviceStateAsync(state);
                HandleStateAction(state);
                _logger.Info($"Set device state: {state}");
            }

        }

        private void HandleStateAction(DeviceStateType state)
        {
            _logger.Info($"Handle state action, state: {state}");
            _stateMachineChangedEvent.SetStaeteChanged(new StateMachineEventArgs(state));
        }

        public async Task<DeviceStateType> GetStateAsync()
        {
            var state = await _twinHandler.GetDeviceStateAsync() ?? GetCurrentDeviceState();
            return state;
        }

        public DeviceStateType GetCurrentDeviceState()
        {
            return currentDeviceState;
        }

    }
}