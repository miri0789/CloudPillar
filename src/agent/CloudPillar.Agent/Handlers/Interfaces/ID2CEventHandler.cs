
namespace CloudPillar.Agent.Handlers;
public interface ID2CMessengerHandler
{
    Task SendFirmwareUpdateEventAsync(string fileName, string actionId, long? startPosition = null, long? endPosition = null);
    Task SendStreamingUploadChunkEventAsync(Stream readStream, string absolutePath, string actionId, long? startPosition = null, long? endPosition = null, CancellationToken cancellationToken = default);
}