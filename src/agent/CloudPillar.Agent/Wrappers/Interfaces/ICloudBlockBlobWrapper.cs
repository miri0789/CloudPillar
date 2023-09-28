using Microsoft.WindowsAzure.Storage.Blob;

namespace CloudPillar.Agent.Wrappers;

public interface ICloudBlockBlobWrapper
{
    CloudBlockBlob CreateCloudBlockBlob(Uri storageUri);
    Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob, Stream source, CancellationToken cancellationToken);
}