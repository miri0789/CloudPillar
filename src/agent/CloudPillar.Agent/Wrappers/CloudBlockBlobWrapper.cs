using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Core.Util;

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

    public async Task DeleteIfExistsAsync(CloudBlockBlob cloudBlockBlob){
        await cloudBlockBlob.DeleteIfExistsAsync();
    }

}
