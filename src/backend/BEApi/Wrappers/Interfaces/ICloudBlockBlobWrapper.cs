using Microsoft.WindowsAzure.Storage.Blob;
namespace Backend.BEApi.Wrappers.Interfaces;

public interface ICloudBlockBlobWrapper
{
    Task UploadFromByteArrayAsync(CloudBlockBlob cloudBlockBlob, byte[] source, int index, int count, CancellationToken cancellationToken);
}