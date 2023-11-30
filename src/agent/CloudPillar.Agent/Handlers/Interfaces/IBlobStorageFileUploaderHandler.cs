using CloudPillar.Agent.Entities;

namespace CloudPillar.Agent.Handlers;

public interface IBlobStorageFileUploaderHandler
{
    Task UploadFromStreamAsync(Uri storageUri, Stream readStream, ActionToReport actionToReport, CancellationToken cancellationToken);
}