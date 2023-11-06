
using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers
{
    public class StateMachineHandler : IStateMachineHandler
    {
        private readonly ITwinHandler _twinHandler;
        private readonly IStateMachineChangedEvent _stateMachineChangedEvent;
        private readonly ILoggerHandler _logger;
        // private readonly IC2DEventHandler _c2DEventHandler;
        // private readonly IStateMachineTokenHandler _stateMachineTokenHandler;

        // public event StateMachineEventHandler StateChanged;

        public StateMachineHandler(
            ITwinHandler twinHandler,
           IStateMachineChangedEvent stateMachineChangedEvent,
         ILoggerHandler logger//,
                              //IC2DEventHandler c2DEventHandler,
                              //IStateMachineTokenHandler stateMachineTokenHandler
         )
        {
            _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
            _stateMachineChangedEvent = stateMachineChangedEvent ?? throw new ArgumentNullException(nameof(stateMachineChangedEvent));
            //_c2DEventHandler = c2DEventHandler ?? throw new ArgumentNullException(nameof(c2DEventHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            //_stateMachineTokenHandler = stateMachineTokenHandler ?? throw new ArgumentNullException(nameof(stateMachineTokenHandler));
        }


        public async Task InitStateMachineHandlerAsync()
        {
            var state = await GetStateAsync();
            _logger.Info($"InitStateMachineHandlerAsync: init device state: {state}");
            HandleStateAction(state);
        }

        public async Task SetStateAsync(DeviceStateType state)
        {
            var currentState = await GetStateAsync();
            if (currentState != state)
            {
                await _twinHandler.UpdateDeviceStateAsync(state);
                HandleStateAction(state);
                _logger.Info($"Set device state: {state}");
            }

        }

        private void HandleStateAction(DeviceStateType state)
        {
            _logger.Info($"Handle state action, state: {state}");
            _stateMachineChangedEvent.SetStaeteChanged(new StateMachineEventArgs(state));

            // switch (state)
            // {
            //     case DeviceStateType.Provisioning:
            //         await SetProvisioningAsync();
            //         break;
            //     case DeviceStateType.Ready:
            //         await SetReadyAsync();
            //         break;
            //     case DeviceStateType.Busy:
            //         SetBusy();
            //         break;
            //     default:
            //         break;
            // }
        }

        public async Task<DeviceStateType> GetStateAsync()
        {
            var state = await _twinHandler.GetDeviceStateAsync() ?? DeviceStateType.Uninitialized;
            return state;
        }

        // private async Task SetProvisioningAsync()
        // {
        //     var _cts = _stateMachineTokenHandler.StartToken();
        //     await _c2DEventHandler.CreateSubscribeAsync(_cts.Token, true);
        // }

        // private async Task SetReadyAsync()
        // {
        //     _stateMachineTokenHandler.CancelToken();
        //     var _cts = _stateMachineTokenHandler.StartToken();
        //     var subscribeTask = _c2DEventHandler.CreateSubscribeAsync(_cts.Token, false);
        //     var handleTwinTask = _twinHandler.HandleTwinActionsAsync(_cts.Token);
        //     await Task.WhenAll(subscribeTask, handleTwinTask);
        // }

        // private void SetBusy()
        // {
        //     _stateMachineTokenHandler.CancelToken();
        // }
    }
}