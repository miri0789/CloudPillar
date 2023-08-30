using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public interface IFileUploaderHandler
{
    Task<ActionToReport> InitFileUploadAsync(UploadAction uploadAction, ActionToReport actionToReport, CancellationToken cancellationToken);
}