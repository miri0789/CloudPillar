using System.Diagnostics;

namespace CloudPillar.Agent.Entities;
public class FileDownload
{
    public ActionToReport action { get; set; }
    public Stopwatch Stopwatch { get; set; }
    public long TotalBytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
}
