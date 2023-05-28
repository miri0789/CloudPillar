﻿using System.ComponentModel;

namespace iotlistener;
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
