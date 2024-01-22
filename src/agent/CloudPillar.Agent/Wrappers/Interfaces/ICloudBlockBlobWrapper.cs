using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Core.Util;

namespace CloudPillar.Agent.Wrappers;

public interface ICloudBlockBlobWrapper
{
    CloudBlockBlob CreateCloudBlockBlob(Uri storageUri);
    Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob, Stream source, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken);
    Task DeleteIfExistsAsync(CloudBlockBlob cloudBlockBlob);
}