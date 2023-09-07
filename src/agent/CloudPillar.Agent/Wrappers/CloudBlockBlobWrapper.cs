using Microsoft.WindowsAzure.Storage.Blob;

namespace CloudPillar.Agent.Wrappers;
public class CloudBlockBlobWrapper : ICloudBlockBlobWrapper
{
    public CloudBlockBlob CreateCloudBlockBlob(Uri storageUri)
    {
        return new CloudBlockBlob(storageUri);
    }
    
    public async Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob, Stream source, CancellationToken cancellationToken)
    {
        await cloudBlockBlob.UploadFromStreamAsync(source);
    }
}
