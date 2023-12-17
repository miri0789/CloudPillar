namespace CloudPillar.Agent.Handlers
{
    public class StateMachineChangedEvent : IStateMachineChangedEvent
    {
        public event StateMachineEventHandler? StateChanged;

        public void SetStateChanged(StateMachineEventArgs args)
        {
             StateChanged?.Invoke(this, args);
        }
    }
}