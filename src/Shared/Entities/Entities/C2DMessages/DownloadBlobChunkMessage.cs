using System.ComponentModel;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

namespace Shared.Entities.Messages;

public class DownloadBlobChunkMessage : C2DMessages
{
    public int RangeIndex { get; set; }
    public int ChunkIndex { get; set; }
    public long Offset { get; set; }
    public string FileName { get; set; }
    public long? RangeSize { get; set; }
    public long FileSize { get; set; }
    public bool FromRunDiagnostics { get; set; } = false;

    public override string GetMessageId()
    {
        return $"{this.FileName}_{this.RangeIndex}_{this.ChunkIndex}";
    }

    public DownloadBlobChunkMessage()
    {
        this.MessageType = C2DMessageType.DownloadChunk;
    }
}

