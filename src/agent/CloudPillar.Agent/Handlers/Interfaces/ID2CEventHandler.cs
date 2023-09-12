
namespace CloudPillar.Agent.Handlers;
public interface ID2CMessengerHandler
{
    Task SendFirmwareUpdateEventAsync(string fileName, string actionId, long? startPosition = null, long? endPosition = null);
    Task SendStreamingUploadChunkEventAsync(Stream readStream, Uri storageUri, string actionId, string correlationId, long startPosition = 0, long? endPosition = null, CancellationToken cancellationToken = default);
}