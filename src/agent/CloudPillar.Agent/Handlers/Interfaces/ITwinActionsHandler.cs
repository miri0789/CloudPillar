using CloudPillar.Agent.Entities;

namespace CloudPillar.Agent.Handlers;

public interface ITwinActionsHandler
{
    Task UpdateReportActionAsync(IEnumerable<ActionToReport> actionsToReport);
}