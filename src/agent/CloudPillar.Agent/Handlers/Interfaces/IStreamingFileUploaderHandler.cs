using CloudPillar.Agent.Entities;
using Microsoft.Azure.Devices.Client.Transport;

namespace CloudPillar.Agent.Handlers;

public interface IStreamingFileUploaderHandler
{
    Task UploadFromStreamAsync(FileUploadCompletionNotification notification, ActionToReport actionToReport, Stream readStream, Uri storageUri, string correlationId, string fileName, CancellationToken cancellationToken, bool isRunDiagnostics = false);
}