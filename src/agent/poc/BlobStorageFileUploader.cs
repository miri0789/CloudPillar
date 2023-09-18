using Microsoft.WindowsAzure.Storage.Blob;

namespace FirmwareUpdateAgent
{
    partial class Program
    {
        public partial class BlobStorageFileUploader : IFileUploader
        {
            private CloudBlockBlob blob;

            public BlobStorageFileUploader(Uri storageUri)
            {
                blob = new CloudBlockBlob(storageUri);
            }

            public async Task UploadFromStreamAsync(CancellationToken cancellationToken, Stream readStream, string correlationId, long startFromPos, Func<bool> cbIsPaused, Func<string, Exception?, Task> onUploadComplete)
            {
                try 
                {
                    using (Stream controllableStream = new ControllableStream(readStream, cancellationToken))
                    {
                        await blob.UploadFromStreamAsync(controllableStream);
                    }
                    // await blob.UploadFromStreamAsync(readStream);
                    await onUploadComplete(correlationId, null);
                } 
                catch(Exception x)
                {
                    await onUploadComplete(correlationId, x);
                }
            }
        }
    }
}
