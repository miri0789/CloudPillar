
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Entities;

public class ActionToReport
{
    public TwinActionReported TwinReport { get; set; }
    public TwinAction TwinAction { get; set; }
    public int ReportIndex { get; set; }
    public string ReportPartName { get; set; }
    public string ChangeSpecId { get; set; }
    public string ChangeSpecKey { get; }

    public ActionToReport(string changeSpecKey, string changeSpecId = "")
    {
        TwinReport = new TwinActionReported();
        TwinAction = new TwinAction();
        ChangeSpecId = changeSpecId;
        ChangeSpecKey = changeSpecKey;
    }
}
