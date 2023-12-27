using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IFileUploaderHandler
{
    Task FileUploadAsync(ActionToReport actionToReport, FileUploadMethod method, string fileName, string changeSpecId, CancellationToken cancellationToken);
    Task UploadFilesToBlobStorageAsync(ActionToReport actionToReport, FileUploadMethod method, string fileName, string changeSpecId, CancellationToken cancellationToken, bool isRunDiagnostics = false);
}