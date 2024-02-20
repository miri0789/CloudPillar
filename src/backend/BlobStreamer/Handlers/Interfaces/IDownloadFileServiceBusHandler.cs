namespace Backend.BlobStreamer.Handlers.Interfaces;

public interface IDownloadFileServiceBusHandler
{
    Task StopProcessingAsync();
    Task StartProcessingAsync();
}