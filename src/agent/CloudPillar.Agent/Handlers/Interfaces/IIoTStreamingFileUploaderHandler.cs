namespace CloudPillar.Agent.Handlers;

public interface IIoTStreamingFileUploaderHandler
{
    Task UploadFromStreamAsync(Stream readStream, long startFromPos, CancellationToken cancellationToken);
}