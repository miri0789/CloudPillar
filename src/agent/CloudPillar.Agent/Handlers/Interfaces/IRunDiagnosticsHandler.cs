using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;
public interface IRunDiagnosticsHandler
{
    Task<string> CreateFileAsync();
    Task<string> UploadFileAsync(string diagnosticsFilePath, CancellationToken cancellationToken);
    Task<TwinActionReported> CheckDownloadStatus(string diagnosticsFilePath, string actionId);
    Task DeleteFileAsync(string diagnosticsFilePath, CancellationToken cancellationToken);
}