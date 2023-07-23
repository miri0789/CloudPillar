using System.Diagnostics;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Entities;
public class FileDownload
{
    
    public DownloadAction TwinAction { get; set; }
    public int TwinReportIndex { get; set; }
    public string TwinPartName { get; set; }
    public Stopwatch Stopwatch { get; set; }
    public long TotalBytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
}
