using CloudPillar.Agent.Entities;

namespace CloudPillar.Agent.Handlers;

public interface IPeriodicUploaderHandler
{
    Task UploadAsync(ActionToReport actionToReport, string changeSpecId, CancellationToken cancellationToken);
}