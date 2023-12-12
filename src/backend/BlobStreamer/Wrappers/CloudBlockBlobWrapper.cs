using Backend.BlobStreamer.Wrappers.Interfaces;
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
        await cloudBlockBlob.UploadFromStreamAsync(source);
    }

    public async Task<MemoryStream> DownloadToStreamAsync(CloudBlockBlob cloudBlockBlob)
    {
        MemoryStream existingData = new MemoryStream();
        await cloudBlockBlob.DownloadToStreamAsync(existingData);

        return existingData;
    }
    public async Task<bool> BlobExists(CloudBlockBlob cloudBlockBlob)
    {
        return await cloudBlockBlob.ExistsAsync();
    }
}
