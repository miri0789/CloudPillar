using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Shared.Entities.QueueMessages;

public class FileDownloadQueueMessage : QueueMessage
{
    public string DeviceId { get; set; }
    public string FileName { get; set; }
    public int ChunkSize { get; set; }
    public long? EndPosition { get; set; }
    public string? ChangeSpecId { get; set; }

    [DefaultValue("")]
    public string CompletedRanges { get; set; }

    [DefaultValue(0)]
    public long StartPosition { get; set; }

    [JsonConstructor]
    public FileDownloadQueueMessage()
    {
        this.MessageType = QueueMessageType.FileDownloadReady;
    }
}