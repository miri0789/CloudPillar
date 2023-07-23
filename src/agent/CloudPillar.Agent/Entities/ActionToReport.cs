
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Entities;

public class ActionToReport
{
    public TwinAction TwinAction { get; set; }
    public int index { get; set; }
    public string ArrayName { get; set; }
}
