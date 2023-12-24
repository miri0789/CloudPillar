
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Entities;

public class ActionToReport
{
    public TwinActionReported TwinReport { get; set; }
    public TwinAction TwinAction { get; set; }
    public int ReportIndex { get; set; }
    public string ReportPartName { get; set; }
    public TwinPatchChangeSpec ChangeSpecKey { get; }
    public bool UploadCompleted { get; set; } = false;

    public ActionToReport(TwinPatchChangeSpec changeSpecKey = TwinPatchChangeSpec.ChangeSpec)
    {
        this.TwinReport = new TwinActionReported();
        this.TwinAction = new TwinAction();
        ChangeSpecKey = changeSpecKey;
    }
}
