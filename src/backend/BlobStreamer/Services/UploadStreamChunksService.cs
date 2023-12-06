using Microsoft.Azure.Storage.Blob;
using Backend.BlobStreamer.Services.Interfaces;
using Shared.Logger;
using Shared.Entities.Services;
using Shared.Entities.Twin;
using Backend.Infra.Common.Services.Interfaces;
using Backend.BlobStreamer.Wrappers.Interfaces;



namespace Backend.BlobStreamer.Services;

public class UploadStreamChunksService : IUploadStreamChunksService
{
    private readonly ILoggerHandler _logger;
    private readonly ICheckSumService _checkSumService;
    private readonly ICloudBlockBlobWrapper _cloudBlockBlobWrapper;
    private readonly ITwinDiseredService _twinDiseredHandler;

    public UploadStreamChunksService(ILoggerHandler logger, ICheckSumService checkSumService, ICloudBlockBlobWrapper cloudBlockBlobWrapper, ITwinDiseredService twinDiseredHandler, IEnvironmentsWrapper @object)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _cloudBlockBlobWrapper = cloudBlockBlobWrapper ?? throw new ArgumentNullException(nameof(cloudBlockBlobWrapper));
        _twinDiseredHandler = twinDiseredHandler ?? throw new ArgumentNullException(nameof(twinDiseredHandler));
    }

    public async Task UploadStreamChunkAsync(Uri storageUri, byte[] readStream, long startPosition, string checkSum, string deviceId, bool isRunDiagnostics, string uploadActionId)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(storageUri);

            long chunkIndex = (startPosition / readStream.Length) + 1;

            _logger.Info($"BlobStreamer: Upload chunk number {chunkIndex}, startPosition: {startPosition}, to {storageUri.AbsolutePath}");

            CloudBlockBlob blob = _cloudBlockBlobWrapper.CreateCloudBlockBlob(storageUri);

            using (Stream inputStream = new MemoryStream(readStream))
            {
                var blobExists = await _cloudBlockBlobWrapper.BlobExists(blob);
                if (!blobExists)
                {
                    await _cloudBlockBlobWrapper.UploadFromStreamAsync(blob, inputStream);
                }
                //continue upload the next stream chunks
                else
                {
                    MemoryStream existingData = await _cloudBlockBlobWrapper.DownloadToStreamAsync(blob);

                    existingData.Seek(startPosition, SeekOrigin.Begin);
                    await inputStream.CopyToAsync(existingData);

                    existingData.Seek(0, SeekOrigin.Begin);
                    // Upload the combined data to the blob
                    await _cloudBlockBlobWrapper.UploadFromStreamAsync(blob, existingData);
                }

                if (!string.IsNullOrEmpty(checkSum))
                {
                    var uploadSuccess = await VerifyStreamChecksum(checkSum, blob);
                    _logger.Info($"isRunDiagnostics: {isRunDiagnostics}");
                    if (uploadSuccess && isRunDiagnostics)
                    {
                        await HandleDownloadForDiagnosticsAsync(deviceId, storageUri, uploadActionId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Blobstreamer UploadFromStreamAsync failed. Message: {ex.Message}");
        }
    }

    private async Task<bool> VerifyStreamChecksum(string originalCheckSum, CloudBlockBlob blob)
    {
        Stream azureStream = new MemoryStream();
        await blob.DownloadToStreamAsync(azureStream);

        string newCheckSum = await _checkSumService.CalculateCheckSumAsync(azureStream);
        var uploadSuccess = newCheckSum.Equals(originalCheckSum);

        if (uploadSuccess)
        {
            _logger.Info($"Blobstreamer UploadFromStreamAsync: File uploaded successfully");
        }
        else
        {
            _logger.Info($"Blobstreamer UploadFromStreamAsync Failed");

            //TO DO
            //add recipe to desired
        }
        return uploadSuccess;
    }

    public async Task HandleDownloadForDiagnosticsAsync(string deviceId, Uri storageUri, string uploadActionId)
    {
        _logger.Info($"preparing download action to add device twin");

        DownloadAction downloadAction = new DownloadAction()
        {
            Action = TwinActionType.SingularDownload,
            ActionId = uploadActionId,
            Description = $"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}",
            Source = Uri.UnescapeDataString(storageUri.Segments.Last()),
            DestinationPath = Path.GetTempFileName(),
        };
        await _twinDiseredHandler.AddDesiredRecipeAsync(deviceId, TwinPatchChangeSpec.ChangeSpecDiagnostics, downloadAction);
    }

}