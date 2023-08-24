
using Shared.Entities.Twin;

namespace CloudPillar.Agent.API.Entities;

public class ActionToReport
{
    public TwinActionReported TwinReport { get; set; }
    public TwinAction TwinAction { get; set; }
    public int ReportIndex { get; set; }
    public string ReportPartName { get; set; }
}
