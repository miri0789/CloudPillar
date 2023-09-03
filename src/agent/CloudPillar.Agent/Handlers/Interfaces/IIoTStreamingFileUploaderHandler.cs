namespace CloudPillar.Agent.Handlers;

public interface IIoTStreamingFileUploaderHandler 
{
    Task UploadFromStreamAsync(Uri storageUri, Stream readStream, long startFromPos, CancellationToken cancellationToken);
}