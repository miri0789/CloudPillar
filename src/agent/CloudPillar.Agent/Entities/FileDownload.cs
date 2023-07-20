using System.Diagnostics;

namespace CloudPillar.Agent.Entities;
public class FileDownload
{
    public Guid ActionGuid { get; set; }
    public Stopwatch Stopwatch { get; set; }
    public string Path { get; set; }
    public string FileName { get; set; }
    public long TotalBytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
}
