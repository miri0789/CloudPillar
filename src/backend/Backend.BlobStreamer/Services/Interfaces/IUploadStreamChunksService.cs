
namespace Backend.BlobStreamer.Interfaces;

public interface IUploadStreamChunksService
{
    Task UploadStreamChunkAsync(Uri storageUri, byte[] readStream, long startPosition, string checkSum);
}