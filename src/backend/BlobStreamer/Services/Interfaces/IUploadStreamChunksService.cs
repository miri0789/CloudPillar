
namespace Backend.BlobStreamer.Services.Interfaces;

public interface IUploadStreamChunksService
{
    Task UploadStreamChunkAsync(Uri storageUri, byte[] readStream, long startPosition, string checkSum, string deviceId, bool isRunDiagnostics);
    Task HandleDownloadForDiagnosticsAsync(string deviceId, Uri storageUri);
}