using System.ComponentModel;

namespace Shared.Entities.Events;

public enum EventType
{
    FirmwareUpdateReady,
    SignTwinKey,
    StreamingUploadChunk
}

public class AgentEvent
{
    public EventType EventType { get; set; }
    public string ActionId { get; set; }
}