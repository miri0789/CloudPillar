namespace CloudPillar.Agent.Handlers
{
    public interface IStateMachineTokenHandler
    {
        CancellationTokenSource StartToken();
        CancellationTokenSource GetToken();
        void CancelToken();

    }
}