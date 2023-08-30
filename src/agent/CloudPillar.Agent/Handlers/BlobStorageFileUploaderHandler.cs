using CloudPillar.Agent.Handlers;
using Microsoft.WindowsAzure.Storage.Blob;

namespace CloudPillar.Agent.Entities
{
    public class BlobStorageFileUploaderHandler : IBlobStorageFileUploaderHandler
    {
        public async Task UploadFromStreamAsync(Uri storageUri, Stream readStream, string correlationId, long startFromPos, Func<string, Exception?, Task> onUploadComplete, CancellationToken cancellationToken)
        {
            CloudBlockBlob blob = new CloudBlockBlob(storageUri);
            try
            {
                using (Stream controllableStream = new ControllableStream(readStream, cancellationToken))
                {
                    await blob.UploadFromStreamAsync(controllableStream);
                }

                await onUploadComplete(correlationId, null);
            }
            catch (Exception x)
            {
                await onUploadComplete(correlationId, x);
            }
        }
    }
}