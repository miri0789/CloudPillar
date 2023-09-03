using System.Collections.Concurrent;

namespace CloudPillar.Agent.Handlers;

public class IoTStreamingFileUploaderHandler : IIoTStreamingFileUploaderHandler
{
    public async Task UploadFromStreamAsync(Uri storageUri, Stream readStream, long startFromPos, CancellationToken cancellationToken)
    {

    }
}
