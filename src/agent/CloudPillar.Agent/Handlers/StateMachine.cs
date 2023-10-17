
using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers
{
    public class StateMachine : IStateMachine
    {
        private readonly ITwinHandler _twinHandler;
        private readonly ILoggerHandler _logger;

        private readonly IC2DEventHandler _c2DEventHandler;
        private CancellationTokenSource _cts;
        private DeviceStateType _currentState;
        public StateMachine(ITwinHandler twinHandler,
         ILoggerHandler logger,
         IC2DEventHandler c2DEventHandler)
        {
            _twinHandler = twinHandler ?? throw new ArgumentNullException(nameof(twinHandler));
            _c2DEventHandler = c2DEventHandler ?? throw new ArgumentNullException(nameof(c2DEventHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        }

        public async Task SetState(DeviceStateType state)
        {
            _currentState = state;
            switch (state)
            {
                case DeviceStateType.Provisioning: await SetProvisioning(); break;
                case DeviceStateType.Ready: await SetReady(); break;
                case DeviceStateType.Busy: SetBusy(); break;
            }
            await _twinHandler.UpdateDeviceStateAsync(state);
            _logger.Info($"Set device state: {state.ToString()}");

        }

        public DeviceStateType GetState()
        {
            return _currentState;
        }

        private async Task SetProvisioning()
        {
            _cts = new CancellationTokenSource();
            _c2DEventHandler.CreateSubscribe(_cts.Token, true);
        }

        private async Task SetReady()
        {
            _cts.Cancel();
            _cts = new CancellationTokenSource();
            _c2DEventHandler.CreateSubscribe(_cts.Token, false);
            await _twinHandler.HandleTwinActionsAsync(_cts.Token);
        }

        private void SetBusy()
        {
            _cts.Cancel();
        }
    }
}