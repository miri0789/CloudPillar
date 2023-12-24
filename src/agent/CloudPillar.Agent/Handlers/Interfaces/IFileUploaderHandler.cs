using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IFileUploaderHandler
{
    Task FileUploadAsync(UploadAction uploadAction, ActionToReport actionToReport, string fileName, string changeSpecId, CancellationToken cancellationToken);
    Task UploadFilesToBlobStorageAsync(string filePathPattern, UploadAction uploadAction, ActionToReport actionToReport, string changeSpecId, CancellationToken cancellationToken, bool isRunDiagnostics = false);
}