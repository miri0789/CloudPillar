namespace CloudPillar.Agent.Handlers;

public interface IIoTStreamingFileUploaderHandler
{
    Task UploadFromStreamAsync(Stream readStream, Uri storageUri, string actionId, string correlationId, CancellationToken cancellationToken);
}