namespace Shared.Entities.QueueMessages;

public enum QueueMessageType
{
    FileDownloadReady,
    SendRangeByChunks
}

public class QueueMessage
{
    public QueueMessageType MessageType { get; set; }
    public int ActionIndex { get; set; }
}