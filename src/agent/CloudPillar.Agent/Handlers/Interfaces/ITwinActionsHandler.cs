using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinActionsHandler
{
    Task UpdateReportedChangeSpecAsync(TwinReportedChangeSpec changeSpec, TwinPatchChangeSpec changeSpecKey, CancellationToken cancellationToken);
    Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReport, CancellationToken cancellationToken);
}