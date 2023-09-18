
namespace CloudPillar.Agent.Handlers;
public interface ID2CMessengerHandler
{
    Task SendFirmwareUpdateEventAsync(string fileName, string actionId, long? startPosition = null, long? endPosition = null);
    Task SendStreamingUploadChunkEventAsync(byte[] readStream, Uri storageUri, string actionId, long currentPosition, int chunkSize, int chunkIndex, int totalChunks, long? endPosition = null, CancellationToken cancellationToken = default);
}