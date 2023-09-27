
namespace Backend.BlobStreamer.Interfaces;

public interface IUploadStreamChunksService
{
    Task UploadStreamChunkAsync(Uri storageUri, string deviceId, byte[] readStream, long startPosition, string checkSum);
}