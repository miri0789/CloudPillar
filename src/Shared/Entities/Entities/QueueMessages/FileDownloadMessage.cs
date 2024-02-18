using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Shared.Entities.QueueMessages;
public class FileDownloadMessage : QueueMessage
{
    public string FileName { get; set; }

    public int ChunkSize { get; set; }

    [DefaultValue("")]
    public string CompletedRanges { get; set; }

    [DefaultValue(0)]
    public long StartPosition { get; set; }

    public long? EndPosition { get; set; }
    public string? ChangeSpecId { get; set; }

    [JsonConstructor]
    public FileDownloadMessage()
    {
        this.MessageType = QueueMessageType.FileDownloadReady;
    }
}