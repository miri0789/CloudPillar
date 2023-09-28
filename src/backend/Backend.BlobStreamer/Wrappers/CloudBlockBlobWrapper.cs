using Backend.BlobStreamer.Interfaces;
using Microsoft.Azure.Storage.Blob;

namespace Backend.BlobStreamer.Wrappers;
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

    public async Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob, Stream source)
    {

        ThrowIfBlobIsNull(cloudBlockBlob);
        await cloudBlockBlob.UploadFromStreamAsync(source);
    }

    public async Task<MemoryStream> DownloadToStreamAsync(CloudBlockBlob cloudBlockBlob)
    {
        ThrowIfBlobIsNull(cloudBlockBlob);
        MemoryStream existingData = new MemoryStream();
        await cloudBlockBlob.DownloadToStreamAsync(existingData);

        return existingData;
    }
    public async Task<bool> BlobExists(CloudBlockBlob cloudBlockBlob)
    {
        ThrowIfBlobIsNull(cloudBlockBlob);
        return await cloudBlockBlob.ExistsAsync();
    }

    private void ThrowIfBlobIsNull(CloudBlockBlob cloudBlockBlob)
    {
        if (cloudBlockBlob == null)
        {
            throw new ArgumentNullException("No cloudBlockBlob was provided for upload");
        }
    }
}
