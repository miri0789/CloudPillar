namespace CloudPillar.Agent.Handlers
{
    public class StateMachineChangedEvent : IStateMachineChangedEvent
    {
        public event StateMachineEventHandler StateChanged;

        public void SetStaeteChanged(StateMachineEventArgs args)
        {
             StateChanged?.Invoke(this, args);
        }
    }
}