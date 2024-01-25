
using Microsoft.WindowsAzure.Storage.Blob;

namespace Backend.BEApi.Wrappers.Interfaces;
public class CloudBlockBlobWrapper : ICloudBlockBlobWrapper
{

    public async Task UploadFromByteArrayAsync(CloudBlockBlob cloudBlockBlob, byte[] source, int index, int count, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(cloudBlockBlob);
        if (!cancellationToken.IsCancellationRequested)
        {
            await cloudBlockBlob.UploadFromByteArrayAsync(source, index, count);
        }
    }
}
