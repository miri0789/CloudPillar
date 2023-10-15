using CloudPillar.Agent.Entities;

namespace CloudPillar.Agent.Handlers;

public interface IStreamingFileUploaderHandler
{
    Task UploadFromStreamAsync(ActionToReport actionToReport, Stream readStream, Uri storageUri, string actionId, string correlationId, CancellationToken cancellationToken);
}