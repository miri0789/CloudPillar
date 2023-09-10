using System.Collections.Concurrent;

namespace CloudPillar.Agent.Handlers;

public class StreamingFileUploaderHandler : IStreamingFileUploaderHandler
{
    public async Task UploadFromStreamAsync(Stream readStream, long startFromPos, CancellationToken cancellationToken)
    {

    }
}
