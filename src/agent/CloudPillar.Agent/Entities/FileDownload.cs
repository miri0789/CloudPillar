using System.Diagnostics;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Entities;
public record FileDownload
{
    public ActionToReport ActionReported { get; init; }
    public Stopwatch Stopwatch { get; set; }
    public long TotalBytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public string TempPath { get; set; }
    public DownloadAction Action
    {
        get => this.ActionReported.TwinAction as DownloadAction;
        set => this.ActionReported.TwinAction = value;
    }
    public TwinActionReported Report
    {
        get => this.ActionReported.TwinReport;
        set => this.ActionReported.TwinReport = value;
    }
}
