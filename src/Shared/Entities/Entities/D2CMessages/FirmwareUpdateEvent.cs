using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Shared.Entities.Messages;
public class FirmwareUpdateEvent : D2CMessage
{
    public string FileName { get; set; }

    public int ChunkSize { get; set; }
    
    [DefaultValue(0)]
    public int RangeIndex { get; set; }

    [DefaultValue(0)]
    public long StartPosition { get; set; }

    public long? EndPosition { get; set; }

    [JsonConstructor]
    public FirmwareUpdateEvent()
    {
        this.MessageType = D2CMessageType.FirmwareUpdateReady;
    }
}