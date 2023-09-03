namespace CloudPillar.Agent.Handlers;

public interface IBlobStorageFileUploaderHandler
{
    Task UploadFromStreamAsync(Uri storageUri, Stream readStream, string correlationId, CancellationToken cancellationToken);
}