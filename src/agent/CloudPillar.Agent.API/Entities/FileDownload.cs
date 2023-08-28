﻿using System.Diagnostics;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.API.Entities;
public record FileDownload
{    
    public ActionToReport Report { get; init; }
    public DownloadAction DownloadAction { get; init; }
    public Stopwatch Stopwatch { get; set; }
    public long TotalBytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
}