using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IFileUploaderHandler
{
    Task FileUploadAsync(UploadAction uploadAction, string fileName, ActionToReport actionToReport, CancellationToken cancellationToken);
}