using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Core.Util;

namespace CloudPillar.Agent.Wrappers;

public interface ICloudBlockBlobWrapper
{
    CloudBlockBlob CreateCloudBlockBlob(Uri storageUri);
    Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob, Stream source, CancellationToken cancellationToken);
    Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob, Stream source, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken);
}