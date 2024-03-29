﻿using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Shared.Entities.Messages;
public class FileDownloadEvent : D2CMessage
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
    public FileDownloadEvent()
    {
        this.MessageType = D2CMessageType.FileDownloadReady;
    }
}