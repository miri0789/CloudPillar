using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinActionsHandler
{
    Task UpdateReportedChangeSpecAsync(TwinReportedChangeSpec changeSpec, TwinPatchChangeSpec changeSpecKey);
    Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReport, CancellationToken cancellationToken, TwinPatchChangeSpec changeSpecKey = TwinPatchChangeSpec.ChangeSpec);
}