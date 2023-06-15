using System.ComponentModel;
using shared.Entities.Enums;

namespace shared.Entities;
public class AgentEvent
{
    public EventType eventType { get; set; }
}

public class FirmwareUpdateEvent : AgentEvent
{
    public string fileName { get; set; }

    public int chunkSize { get; set; }
    
    [DefaultValue(0)]
    public long startPosition { get; set; }
}

public class SignEvent : AgentEvent
{
    public string keyPath { get; set; }

    public string signatureKey { get; set; }
}