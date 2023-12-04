using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Core.Util;

namespace CloudPillar.Agent.Wrappers;
public class CloudBlockBlobWrapper : ICloudBlockBlobWrapper
{
    public CloudBlockBlob CreateCloudBlockBlob(Uri storageUri)
    {
        ArgumentNullException.ThrowIfNull(storageUri);
        return new CloudBlockBlob(storageUri);
    }

    public async Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob, Stream source, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cloudBlockBlob);

        await cloudBlockBlob.UploadFromStreamAsync(
                  source,
                  default(AccessCondition),
                  default(BlobRequestOptions),
                  default(OperationContext),
                  progressHandler,
                  cancellationToken
                  );
    }

}
