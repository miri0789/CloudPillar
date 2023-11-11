using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IFileUploaderHandler
{
    Task UploadFilesToBlobStorageAsync(string filePathPattern, UploadAction uploadAction, ActionToReport actionToReport, CancellationToken cancellationToken);
}