namespace CloudPillar.Agent.Handlers;

public interface IIoTStreamingFileUploaderHandler
{
    Task UploadFromStreamAsync(Stream readStream, string absolutePath, string actionId);
}