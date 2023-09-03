using CloudPillar.Agent.Handlers;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CloudPillar.Agent.Entities
{
    public class BlobStorageFileUploaderHandler : IBlobStorageFileUploaderHandler
    {
        public async Task UploadFromStreamAsync(Uri storageUri, Stream readStream, string correlationId, CancellationToken cancellationToken)
        {
            CloudBlockBlob blob = new CloudBlockBlob(storageUri);

            using (Stream controllableStream = new StreamWrapper(readStream, cancellationToken))
            {
                await blob.UploadFromStreamAsync(controllableStream);
            }
        }
    }
}