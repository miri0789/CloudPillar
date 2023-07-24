namespace Shared.Entities.Events;


public class SignEvent : AgentEvent
{
    public string KeyPath { get; set; }

    public string SignatureKey { get; set; }

    public SignEvent()
    {
        this.EventType = EventType.SignTwinKey;
    }
}