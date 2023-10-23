
using CloudPillar.Agent.Entities;
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
        public StateMachineHandler(ITwinHandler twinHandler,
         ILoggerHandler logger,
         IC2DEventHandler c2DEventHandler,
         IStateMachineTokenHandler stateMachineTokenHandler)
        {
            _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
            _c2DEventHandler = c2DEventHandler ?? throw new ArgumentNullException(nameof(c2DEventHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _stateMachineTokenHandler = stateMachineTokenHandler ?? throw new ArgumentNullException(nameof(stateMachineTokenHandler));
        }


        public async Task InitStateMachineHandler()
        {
            var state = await GetState();
            _logger.Info($"InitStateMachineHandler: init device state: {state.ToString()}");
            await HandleStateAction(state);
        }

        public async Task SetState(DeviceStateType state)
        {
            var currentState = await GetState();
            if (currentState != state)
            {
                await _twinHandler.UpdateDeviceStateAsync(state);
                await HandleStateAction(state);
                _logger.Info($"Set device state: {state.ToString()}");
            }

        }

        private async Task HandleStateAction(DeviceStateType state)
        {
            switch (state)
            {
                case DeviceStateType.Provisioning: await SetProvisioning(); break;
                case DeviceStateType.Ready: await SetReady(); break;
                case DeviceStateType.Busy: SetBusy(); break;
                default: break;
            }
        }

        public async Task<DeviceStateType> GetState()
        {
            var state = await _twinHandler.GetDeviceStateAsync() ?? DeviceStateType.Uninitialized;
            return state;
        }

        private async Task SetProvisioning()
        {
            var _cts = _stateMachineTokenHandler.StartToken();
            var result = await _c2DEventHandler.CreateProvisioningSubscribe(_cts.Token);
            if (result)
            {
                await SetState(DeviceStateType.Ready);
            }
        }

        private async Task SetReady()
        {
            _stateMachineTokenHandler.CancelToken();
             var _cts = _stateMachineTokenHandler.StartToken();
            _c2DEventHandler.CreateSubscribe(_cts.Token);
            await _twinHandler.HandleTwinActionsAsync(_cts.Token);
        }

        private void SetBusy()
        {
            _stateMachineTokenHandler.CancelToken();
        }
    }
}