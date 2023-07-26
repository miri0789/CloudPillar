using System.Diagnostics;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Entities;
public class FileDownload
{    
    public ActionToReport Report { get; set; }
    public DownloadAction DownloadAction { get; set; }
    public Stopwatch Stopwatch { get; set; }
    public long TotalBytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
}
