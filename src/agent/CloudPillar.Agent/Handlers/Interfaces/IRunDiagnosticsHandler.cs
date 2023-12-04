using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;
public interface IRunDiagnosticsHandler
{
    Task<TwinActionReported> HandleRunDiagnosticsProcess(CancellationToken cancellationToken);
}