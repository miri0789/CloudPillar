using System.ComponentModel;

namespace shared.Entities.Events;

public enum EventType
{
    FirmwareUpdateReady,
    SignTwinKey
}

public abstract class AgentEvent
{
    public abstract EventType EventType { get; set; }
    public Guid ActionGuid { get; set; }
}