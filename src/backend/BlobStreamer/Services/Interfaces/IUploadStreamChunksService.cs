using Microsoft.Azure.Storage.Blob;

namespace Backend.BlobStreamer.Services.Interfaces;

public interface IUploadStreamChunksService
{
    Task UploadStreamChunkAsync(Uri storageUri, byte[] readStream, long startPosition, string checkSum, string fileName, string deviceId, bool isRunDiagnostics = false);
    Task HandleDownloadForDiagnosticsAsync(string deviceId, Uri storageUri, CloudBlockBlob blob);
}