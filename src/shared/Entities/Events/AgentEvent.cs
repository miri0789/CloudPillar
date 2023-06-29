using System.ComponentModel;
using shared.Entities.Enums;

namespace shared.Entities.Events;
public class AgentEvent
{
    public EventType EventType { get; set; }
    public Guid ActionGuid { get; set; }
}

public class FirmwareUpdateEvent : AgentEvent
{
    public string FileName { get; set; }

    public int ChunkSize { get; set; }
    
    [DefaultValue(0)]
    public long StartPosition { get; set; }
}

public class SignEvent : AgentEvent
{
    public string KeyPath { get; set; }

    public string SignatureKey { get; set; }
}