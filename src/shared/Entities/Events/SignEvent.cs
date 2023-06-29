namespace shared.Entities.Events;


public class SignEvent : AgentEvent
{
    public override EventType EventType
    {
        get { return EventType.SignTwinKey; }
        set { EventType = EventType.SignTwinKey; }
    }

    public string KeyPath { get; set; }

    public string SignatureKey { get; set; }
}