using Microsoft.Azure.Storage.Blob;
using Backend.BlobStreamer.Interfaces;
using Shared.Logger;
using Shared.Entities.Services;

namespace Backend.BlobStreamer.Services;

public class UploadStreamChunksService : IUploadStreamChunksService
{
    private readonly ILoggerHandler _logger;
    private readonly ICheckSumService _checkSumService;
    private readonly ICloudBlockBlobWrapper _cloudBlockBlobWrapper;

    public UploadStreamChunksService(ILoggerHandler logger, ICheckSumService checkSumService, ICloudBlockBlobWrapper cloudBlockBlobWrapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService)); ;
        _cloudBlockBlobWrapper = cloudBlockBlobWrapper ?? throw new ArgumentNullException(nameof(cloudBlockBlobWrapper)); ;
    }

    public async Task UploadStreamChunkAsync(Uri storageUri, byte[] readStream, long startPosition, string checkSum)
    {
        try
        {
            if (storageUri == null)
            {
                throw new ArgumentNullException("No storage uri was provied");
            }

            long chunkIndex = (startPosition / readStream.Length) + 1;

            _logger.Info($"BlobStreamer: Upload chunk number {chunkIndex} to {storageUri.AbsolutePath}");

            CloudBlockBlob blob = _cloudBlockBlobWrapper.CreateCloudBlockBlob(storageUri);

            using (Stream inputStream = new MemoryStream(readStream))
            {
                var blobExists = await _cloudBlockBlobWrapper.BlobExists(blob);
                //first chunk
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

                    // Reset the position of existingData to the beginning
                    existingData.Seek(0, SeekOrigin.Begin);

                    // Upload the combined data to the blob
                    await _cloudBlockBlobWrapper.UploadFromStreamAsync(blob, inputStream);

                    if (!string.IsNullOrEmpty(checkSum))
                    {
                        await VerifyStreamChecksum(checkSum, blob);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Blobstreamer UploadFromStreamAsync failed. Message: {ex.Message}");
        }
    }


    private async Task VerifyStreamChecksum(string originalCheckSum, CloudBlockBlob blob)
    {
        Stream azureStream = new MemoryStream();
        await blob.DownloadToStreamAsync(azureStream);

        string newCheckSum = await _checkSumService.CalculateCheckSumAsync(azureStream);
        var uploadSuccess = newCheckSum.Equals(originalCheckSum);

        if (uploadSuccess)
        {
            _logger.Debug($"Blobstreamer UploadFromStreamAsync: File uploaded successfully");
        }
        else
        {
            _logger.Debug($"Blobstreamer UploadFromStreamAsync Failed");
            
            //TO DO
            //add recipe to desired
        }


    }
}