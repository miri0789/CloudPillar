
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers
{
    public class StateMachineHandler : IStateMachineHandler
    {
        private readonly ITwinHandler _twinHandler;
        private readonly ILoggerHandler _logger;

        private readonly IC2DEventHandler _c2DEventHandler;
        private readonly IStateMachineTokenHandler _stateMachineTokenHandler;
        private readonly IDeviceClientWrapper _deviceClientWrapper;
        private static DeviceStateType currentDeviceState = DeviceStateType.Uninitialized;
        public StateMachineHandler(ITwinHandler twinHandler,
         ILoggerHandler logger,
         IC2DEventHandler c2DEventHandler,
         IStateMachineTokenHandler stateMachineTokenHandler,
         IDeviceClientWrapper deviceClientWrapper)
        {
            _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
            _c2DEventHandler = c2DEventHandler ?? throw new ArgumentNullException(nameof(c2DEventHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stateMachineTokenHandler = stateMachineTokenHandler ?? throw new ArgumentNullException(nameof(stateMachineTokenHandler));
            _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        }


        public async Task InitStateMachineHandlerAsync()
        {
            var state = await GetStateAsync();
            _logger.Info($"InitStateMachineHandlerAsync: init device state: {state}");
            await HandleStateAction(state);
        }

        public async Task SetStateAsync(DeviceStateType state)
        {
            var currentState = await GetStateAsync();
            if (currentState != state)
            {
                currentDeviceState = state;
                await _twinHandler.UpdateDeviceStateAsync(state);
                await HandleStateAction(state);
                _logger.Info($"Set device state: {state}");
            }

        }

        private async Task HandleStateAction(DeviceStateType state)
        {
            _logger.Info($"Handle state action, state: {state}");
            switch (state)
            {
                case DeviceStateType.Provisioning:
                    await SetProvisioningAsync();
                    break;
                case DeviceStateType.Ready:
                    await SetReadyAsync();
                    break;
                case DeviceStateType.Busy:
                    await SetBusyAsync();
                    break;
                default:
                    break;
            }
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

        private async Task SetProvisioningAsync()
        {
            var _cts = _stateMachineTokenHandler.StartToken();
            await _c2DEventHandler.CreateSubscribeAsync(_cts.Token, true);
        }

        private async Task SetReadyAsync()
        {
            _stateMachineTokenHandler.CancelToken();
            var _cts = _stateMachineTokenHandler.StartToken();
            var subscribeTask = _c2DEventHandler.CreateSubscribeAsync(_cts.Token, false);
            var handleTwinTask = _twinHandler.HandleTwinActionsAsync(_cts.Token);
            await Task.WhenAll(subscribeTask, handleTwinTask);
        }

        private async Task SetBusyAsync()
        {
            await _twinHandler.SaveLastTwinAsync();
            await _deviceClientWrapper.DisposeAsync();
            _stateMachineTokenHandler.CancelToken();
        }
    }
}