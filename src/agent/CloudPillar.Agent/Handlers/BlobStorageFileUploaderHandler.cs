using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.WindowsAzure.Storage.Blob;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.WindowsAzure.Storage.Core.Util;

namespace CloudPillar.Agent.Handlers
{
    public class BlobStorageFileUploaderHandler : IBlobStorageFileUploaderHandler
    {
        private readonly ICloudBlockBlobWrapper _cloudBlockBlobWrapper;
        private readonly ITwinReportHandler _twinReportHandler;
        private readonly ILoggerHandler _logger;

        public BlobStorageFileUploaderHandler(ICloudBlockBlobWrapper cloudBlockBlobWrapper, ITwinReportHandler twinReportHandler, ILoggerHandler logger)
        {
            _cloudBlockBlobWrapper = cloudBlockBlobWrapper ?? throw new ArgumentNullException(nameof(cloudBlockBlobWrapper));
            _twinReportHandler = twinReportHandler ?? throw new ArgumentNullException(nameof(twinReportHandler));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task UploadFromStreamAsync(FileUploadCompletionNotification notification, Uri storageUri, Stream readStream, ActionToReport actionToReport, string fileName, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                ArgumentNullException.ThrowIfNull(storageUri);

                CloudBlockBlob cloudBlockBlob = _cloudBlockBlobWrapper.CreateCloudBlockBlob(storageUri);

                IProgress<StorageProgress> progressHandler = new Progress<StorageProgress>(
                    async progress =>
                                await SetReportProggress(progress.BytesTransferred, readStream.Length, actionToReport, notification, fileName, cancellationToken)
                   );
                await _cloudBlockBlobWrapper.UploadFromStreamAsync(cloudBlockBlob, readStream, progressHandler, cancellationToken);
            }
        }

        private async Task SetReportProggress(long bytesTransferred, long totalSize, ActionToReport actionToReport, FileUploadCompletionNotification notification, string fileName, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                float progressPercent = (totalSize > 0) ? (float)Math.Floor(bytesTransferred / (double)totalSize * 100 * 100) / 100 : 100;
                var twinReport = _twinReportHandler.GetActionToReport(actionToReport, fileName);
                twinReport.Progress = progressPercent;
                twinReport.CorrelationId = notification.CorrelationId;
                twinReport.Status = StatusType.InProgress;

                _logger.Info($"Percentage uploaded: {progressPercent}%");
                await _twinReportHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);
            }
        }
    }
}