using System.Diagnostics;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Entities;
public record FileDownload
{
    public required ActionToReport ActionReported { get; init; }
    public Stopwatch Stopwatch { get; set; } = new Stopwatch();
    public long TotalBytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public DownloadAction Action
    {
        get => (ActionReported.TwinAction as DownloadAction)!;
        set => ActionReported.TwinAction = value;
    }
    public TwinActionReported Report
    {
        get => ActionReported.TwinReport;
        set => ActionReported.TwinReport = value;
    }
}
