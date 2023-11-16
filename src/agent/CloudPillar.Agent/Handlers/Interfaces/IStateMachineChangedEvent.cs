namespace CloudPillar.Agent.Handlers
{
    public interface IStateMachineChangedEvent
    {
        event StateMachineEventHandler StateChanged;

         void SetStaeteChanged(StateMachineEventArgs args);
    }
}