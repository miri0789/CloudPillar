using Microsoft.WindowsAzure.Storage.Blob;

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

    public async Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob, Stream source, CancellationToken cancellationToken)
    {
        if(cloudBlockBlob == null){
            throw new ArgumentNullException("No cloudBlockBlob was provided for upload");
        }
        await cloudBlockBlob.UploadFromStreamAsync(source);
    }
}
