namespace CloudPillar.Agent.Handlers;

public interface IStreamingFileUploaderHandler
{
    Task UploadFromStreamAsync(Stream readStream, Uri storageUri, string actionId, string correlationId, CancellationToken cancellationToken);
}