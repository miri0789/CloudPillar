namespace CloudPillar.Agent.Handlers;

public interface IBlobStorageFileUploaderHandler : IUploadFromStreamHandler
{
    Task UploadFromStreamAsync(Uri storageUri, Stream readStream, string correlationId, long startFromPos, Func<string, Exception?, Task> onUploadComplete, CancellationToken cancellationToken);
}