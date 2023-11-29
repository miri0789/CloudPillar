using System.Reflection.Metadata.Ecma335;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Core.Util;

namespace CloudPillar.Agent.Handlers
{
    public class BlobStorageFileUploaderHandler : IBlobStorageFileUploaderHandler
    {
        private readonly ICloudBlockBlobWrapper _cloudBlockBlobWrapper;
        private readonly ITwinActionsHandler _twinActionsHandler;

        public BlobStorageFileUploaderHandler(ICloudBlockBlobWrapper cloudBlockBlobWrapper, ITwinActionsHandler twinActionsHandler)
        {
            ArgumentNullException.ThrowIfNull(cloudBlockBlobWrapper);
            _cloudBlockBlobWrapper = cloudBlockBlobWrapper;
            this.twinActionsHandler = twinActionsHandler;
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
                IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(
                    progress =>
                                SetProggress(progress.BytesTransferred, controllableStream.Length)
                   );

                await _cloudBlockBlobWrapper.UploadFromStreamAsync(cloudBlockBlob, controllableStream, progressHandler, cancellationToken);
            }
        }

        private async Task SetProggress(long bytesTransferred, long totalSize)
        {
            var progressPercent = (float)Math.Round(bytesTransferred / (double)totalSize * 100, 2);
            await _twinActionsHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);
        }
    }
}