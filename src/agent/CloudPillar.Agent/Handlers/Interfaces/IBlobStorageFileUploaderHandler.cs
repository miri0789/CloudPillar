using CloudPillar.Agent.Entities;
using Microsoft.Azure.Devices.Client.Transport;

namespace CloudPillar.Agent.Handlers;

public interface IBlobStorageFileUploaderHandler
{
    Task UploadFromStreamAsync(FileUploadCompletionNotification notification, Uri storageUri, Stream readStream, ActionToReport actionToReport, CancellationToken cancellationToken);
}