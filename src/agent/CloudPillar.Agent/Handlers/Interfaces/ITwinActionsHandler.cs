using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface ITwinActionsHandler
{
    Task UpdateReportedChangeSpecAsync(TwinReportedChangeSpec changeSpec, CancellationToken cancellationToken);
    Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReport, CancellationToken cancellationToken);
}