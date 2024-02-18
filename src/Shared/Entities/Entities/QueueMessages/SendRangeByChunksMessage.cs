namespace Shared.Entities.QueueMessages;

public class SendRangeByChunksMessage : QueueMessage
{
    public int RangeIndex { get; set; }
    public string FileName { get; set; }
    public int ChunkSize { get; set; }
    public int RangeSize { get; set; }
    public long RangeStartPosition { get; set; }
    public int? RangesCount { get; set; }
    public string? ChangeSpecId { get; set; }

    public SendRangeByChunksMessage()
    {
        this.MessageType = QueueMessageType.SendRangeByChunks;
    }
}