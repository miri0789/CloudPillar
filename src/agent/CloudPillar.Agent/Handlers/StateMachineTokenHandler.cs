
using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers
{
    public class StateMachineTokenHandler : IStateMachineTokenHandler
    {
        private CancellationTokenSource _cts;


        public CancellationTokenSource StartToken()
        {
            _cts = new CancellationTokenSource();
            return _cts;
        }

        public CancellationTokenSource GetToken()
        {
            return _cts;
        }

        public void CancelToken()
        {           
            _cts?.Cancel();
        }

    }
}