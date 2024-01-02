namespace CloudPillar.Agent.Handlers
{
    public interface IStateMachineChangedEvent
    {
        event StateMachineEventHandler StateChanged;

         void SetStateChanged(StateMachineEventArgs args);
    }
}