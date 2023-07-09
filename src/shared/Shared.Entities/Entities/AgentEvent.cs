using System.ComponentModel;
using shared.Entities.Enums;

namespace shared.Entities;
public class AgentEvent
{
    public EventType EventType { get; set; }
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
    public string keyPath { get; set; }

    public string signatureKey { get; set; }
}