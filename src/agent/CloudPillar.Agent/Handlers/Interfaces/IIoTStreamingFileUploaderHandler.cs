namespace CloudPillar.Agent.Handlers;

public interface IStreamingFileUploaderHandler
{
    Task UploadFromStreamAsync(Stream readStream, long startFromPos, CancellationToken cancellationToken);
}