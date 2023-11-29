using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Core.Util;

namespace CloudPillar.Agent.Wrappers;
public class CloudBlockBlobWrapper : ICloudBlockBlobWrapper
{
    public CloudBlockBlob CreateCloudBlockBlob(Uri storageUri)
    {
        if (storageUri == null)
        {
            throw new ArgumentNullException("No URI was provided for creation a CloudBlockBlob");
        }
        return new CloudBlockBlob(storageUri);
    }

      public async Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob, Stream source, IProgress<StorageProgress> progressHandler, CancellationToken cancellationToken)
    {
        if (cloudBlockBlob == null)
        {
            throw new ArgumentNullException("No cloudBlockBlob was provided for upload");
        }
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
