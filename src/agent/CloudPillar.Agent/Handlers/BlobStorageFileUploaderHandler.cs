using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Storage.Blob;

namespace CloudPillar.Agent.Entities
{
    public class BlobStorageFileUploaderHandler : IBlobStorageFileUploaderHandler
    {
        private readonly ICloudBlockBlobWrapper _cloudBlockBlobWrapper;
        public BlobStorageFileUploaderHandler(ICloudBlockBlobWrapper cloudBlockBlobWrapper)
        {
            ArgumentNullException.ThrowIfNull(cloudBlockBlobWrapper);
            _cloudBlockBlobWrapper = cloudBlockBlobWrapper;
        }

        public async Task UploadFromStreamAsync(Uri storageUri, Stream readStream, CancellationToken cancellationToken)
        {
            if (storageUri == null)
            {
                throw new ArgumentNullException("No URI was provided for upload the stream");
            }
            CloudBlockBlob cloudBlockBlob = _cloudBlockBlobWrapper.CreateCloudBlockBlob(storageUri);
            using (Stream controllableStream = new StreamWrapper(readStream, cancellationToken))
            {
                await _cloudBlockBlobWrapper.UploadFromStreamAsync(cloudBlockBlob, controllableStream, cancellationToken);
            }
        }
    }
}