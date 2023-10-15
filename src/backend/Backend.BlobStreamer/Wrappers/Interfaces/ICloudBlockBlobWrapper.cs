using Microsoft.Azure.Storage.Blob;

namespace Backend.BlobStreamer.Interfaces;

public interface ICloudBlockBlobWrapper
{
    CloudBlockBlob CreateCloudBlockBlob(Uri storageUri);
    Task UploadFromStreamAsync(CloudBlockBlob cloudBlockBlob, Stream source);
    Task<MemoryStream> DownloadToStreamAsync(CloudBlockBlob cloudBlockBlob);
    Task<bool> BlobExists(CloudBlockBlob cloudBlockBlob);
}