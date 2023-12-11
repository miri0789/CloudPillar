namespace Shared.Entities.Messages;

public class DownloadBlobChunkMessage : C2DMessages
{
    public int RangeIndex { get; set; }
    public int ChunkIndex { get; set; }
    public long Offset { get; set; }
    public string FileName { get; set; }   
    public long FileSize { get; set; }
    public long? RangeStartPosition { get; set; }
    public long? RangeEndPosition { get; set; }
    public string RangeCheckSum { get; set; }
    public int? RangesCount { get; set; }
    public override string GetMessageId()
    {
        return $"{this.FileName}_{this.RangeIndex}_{this.ChunkIndex}";
    }

    public DownloadBlobChunkMessage()
    {
        this.MessageType = C2DMessageType.DownloadChunk;
    }
}

