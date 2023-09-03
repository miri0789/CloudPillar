namespace CloudPillar.Agent.Handlers;

public interface IIoTStreamingFileUploaderHandler 
{
    Task UploadFromStreamAsync(Uri storageUri, Stream readStream, string correlationId, long startFromPos, CancellationToken cancellationToken);
}