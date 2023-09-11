
using CloudPillar.Agent.Wrappers;

namespace CloudPillar.Agent.Handlers;

public class IoTStreamingFileUploaderHandler : IIoTStreamingFileUploaderHandler
{
    private readonly ID2CMessengerHandler _d2CMessengerHandler;

    public IoTStreamingFileUploaderHandler(ID2CMessengerHandler d2CMessengerHandler)
    {
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
    }

    public async Task UploadFromStreamAsync(Stream readStream, string absolutePath, string actionId, string correlationId)
    {
        await _d2CMessengerHandler.SendStreamingUploadChunkEventAsync(readStream, absolutePath, actionId, correlationId, 0);
    }
}
