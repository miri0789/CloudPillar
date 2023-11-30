using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Core.Util;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers
{
    public class BlobStorageFileUploaderHandler : IBlobStorageFileUploaderHandler
    {
        private readonly ICloudBlockBlobWrapper _cloudBlockBlobWrapper;
        private readonly ITwinActionsHandler _twinActionsHandler;
        private readonly ILoggerHandler _logger;

        public BlobStorageFileUploaderHandler(ICloudBlockBlobWrapper cloudBlockBlobWrapper, ITwinActionsHandler twinActionsHandler, ILoggerHandler logger)
        {
            _cloudBlockBlobWrapper = cloudBlockBlobWrapper ?? throw new ArgumentNullException(nameof(cloudBlockBlobWrapper));
            _twinActionsHandler = twinActionsHandler ?? throw new ArgumentNullException(nameof(twinActionsHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task UploadFromStreamAsync(Uri storageUri, Stream readStream, ActionToReport actionToReport, CancellationToken cancellationToken)
        {
            if (storageUri == null)
            {
                throw new ArgumentNullException("No URI was provided for upload the stream");
            }
            CloudBlockBlob cloudBlockBlob = _cloudBlockBlobWrapper.CreateCloudBlockBlob(storageUri);

            IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(
                async progress =>
                            await SetReportProggress(progress.BytesTransferred, readStream.Length, actionToReport, cancellationToken)
               );
            await _cloudBlockBlobWrapper.UploadFromStreamAsync(cloudBlockBlob, readStream, progressHandler, cancellationToken);
        }

        private async Task SetReportProggress(long bytesTransferred, long totalSize, ActionToReport actionToReport, CancellationToken cancellationToken)
        {
            var progressPercent = (float)Math.Round(bytesTransferred / (double)totalSize * 100, 2);

            actionToReport.TwinReport.Status = StatusType.InProgress;
            actionToReport.TwinReport.Progress = progressPercent;
            _logger.Info($"Percentage uploaded: {progressPercent}%");

            await _twinActionsHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);
        }
    }
}