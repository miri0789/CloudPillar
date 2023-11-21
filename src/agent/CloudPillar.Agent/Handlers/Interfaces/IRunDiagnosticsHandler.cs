using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;
public interface IRunDiagnosticsHandler
{
    Task CreateFileAsync();
    Task<string> UploadFileAsync(CancellationToken cancellationToken);
    Task WaitForResponse(string actionId);
}