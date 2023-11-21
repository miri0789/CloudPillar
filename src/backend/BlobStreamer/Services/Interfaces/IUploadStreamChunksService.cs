
namespace Backend.BlobStreamer.Interfaces;

public interface IUploadStreamChunksService
{
    Task UploadStreamChunkAsync(Uri storageUri, byte[] readStream, long startPosition, string checkSum, string deviceId, bool fromRunDiagnostic, string uploadActionId);
    Task HandleDownloadForDiagnosticsAsync(string deviceId, Uri storageUri, string uploadActionId);
}