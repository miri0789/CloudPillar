
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Entities;

public class ActionToReport
{
    public TwinAction TwinAction { get; set; }
    public int TwinReportIndex { get; set; }
    public string TwinPartName { get; set; }
}
