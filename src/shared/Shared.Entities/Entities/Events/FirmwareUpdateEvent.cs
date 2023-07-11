using System.ComponentModel;
using System.Text.Json.Serialization;
using shared.Entities.Events;

public class FirmwareUpdateEvent : AgentEvent
{
    public string FileName { get; set; }

    public int ChunkSize { get; set; }

    [DefaultValue(0)]
    public long StartPosition { get; set; }

    public long? EndPosition { get; set; }


    [JsonConstructor]
    public FirmwareUpdateEvent()
    {
        this.EventType = EventType.FirmwareUpdateReady;
    }
}