using System.ComponentModel;
using shared.Entities.Events;

public class FirmwareUpdateEvent : AgentEvent
{
    public string FileName { get; set; }

    public int ChunkSize { get; set; }

    [DefaultValue(0)]
    public long StartPosition { get; set; }

    public long? EndPosition { get; set; }

    public FirmwareUpdateEvent()
    {
        this.EventType = EventType.FirmwareUpdateReady;
    }
}