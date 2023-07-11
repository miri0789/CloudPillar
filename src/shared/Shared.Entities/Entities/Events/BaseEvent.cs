using System.ComponentModel;

namespace shared.Entities.Events;

public enum EventType
{
    FirmwareUpdateReady,
    SignTwinKey
}

public class AgentEvent
{
    public EventType EventType { get; set; }
    public Guid ActionGuid { get; set; }
}