

namespace CloudPillar.Agent.Handlers;
public interface IRunDiagnosticsHandler
{
    Task CreateFileAsync();
    Task UploadFileAsync(CancellationToken cancellationToken);

}