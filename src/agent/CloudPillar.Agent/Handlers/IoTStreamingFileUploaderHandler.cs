using System.Collections.Concurrent;

namespace CloudPillar.Agent.Handlers;

public class IoTStreamingFileUploaderHandler : IIoTStreamingFileUploaderHandler
{
    private ConcurrentDictionary<string, Func<string, string?, Task>> _uploadCompletionHandlers = new ConcurrentDictionary<string, Func<string, string?, Task>>();

    public IoTStreamingFileUploaderHandler()
    {

    }

    public async Task UploadFromStreamAsync(Uri storageUri, Stream readStream, string correlationId, long startFromPos, Func<string, Exception?, Task> onUploadComplete, CancellationToken cancellationToken)
    {

    }
}
