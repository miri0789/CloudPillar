using System.ComponentModel;
using shared.Entities.Events;

public class FirmwareUpdateEvent : AgentEvent
{
    public override EventType EventType
    {
        get { return EventType.FirmwareUpdateReady; }
        set { EventType = EventType.FirmwareUpdateReady; }
    }

    public string FileName { get; set; }

    public int ChunkSize { get; set; }

    [DefaultValue(0)]
    public long StartPosition { get; set; }
}