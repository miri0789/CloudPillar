using Microsoft.Azure.Storage.Blob;
using Backend.BlobStreamer.Services.Interfaces;
using Shared.Logger;
using Shared.Entities.Services;
using Shared.Entities.Twin;
using Backend.Infra.Common.Services.Interfaces;
using Backend.BlobStreamer.Wrappers.Interfaces;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using Shared.Entities.Utilities;



namespace Backend.BlobStreamer.Services;

public class UploadStreamChunksService : IUploadStreamChunksService
{
    private readonly CloudBlobContainer _container;
    private readonly ILoggerHandler _logger;
    private readonly ICheckSumService _checkSumService;
    private readonly ICloudBlockBlobWrapper _cloudBlockBlobWrapper;
    private readonly ITwinDiseredService _twinDiseredHandler;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly ISHA256Wrapper _sha256Wrapper;
    private readonly IChangeSpecService _changeSpecService;

    public UploadStreamChunksService(ILoggerHandler logger, ICheckSumService checkSumService, ICloudBlockBlobWrapper cloudBlockBlobWrapper,
      ICloudStorageWrapper cloudStorageWrapper, ITwinDiseredService twinDiseredHandler, IEnvironmentsWrapper environmentsWrapper,
      ISHA256Wrapper sha256Wrapper, IChangeSpecService changeSpecService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _cloudBlockBlobWrapper = cloudBlockBlobWrapper ?? throw new ArgumentNullException(nameof(cloudBlockBlobWrapper));
        _twinDiseredHandler = twinDiseredHandler ?? throw new ArgumentNullException(nameof(twinDiseredHandler));
        _environmentsWrapper = environmentsWrapper ?? throw new ArgumentNullException(nameof(environmentsWrapper));
        _sha256Wrapper = sha256Wrapper ?? throw new ArgumentNullException(nameof(sha256Wrapper));
        _changeSpecService = changeSpecService ?? throw new ArgumentNullException(nameof(changeSpecService));
        _container = cloudStorageWrapper.GetBlobContainer(_environmentsWrapper.storageConnectionString, _environmentsWrapper.blobContainerName);
    }

    public async Task UploadStreamChunkAsync(Uri storageUri, byte[] readStream, long startPosition, string checkSum, string fileName, string deviceId, bool isRunDiagnostics = false)
    {
        try
        {
            CloudBlockBlob blob;

            if (storageUri == null)
            {
                _logger.Info($"BlobStreamer: storageUri is null, get storageUri from storage connectionstring");
                blob = _container.GetBlockBlobReference(fileName);
            }
            else
            {
                blob = _cloudBlockBlobWrapper.CreateCloudBlockBlob(storageUri);
            }
            long chunkIndex = (startPosition / readStream.Length) + 1;

            _logger.Info($"BlobStreamer: Upload chunk number {chunkIndex}, startPosition: {startPosition}, to {blob.Uri.AbsolutePath}");

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
                        await HandleDownloadForDiagnosticsAsync(deviceId, storageUri, readStream);
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

    public async Task HandleDownloadForDiagnosticsAsync(string deviceId, Uri storageUri, byte[] readStream)
    {
        _logger.Info($"preparing download action to add device twin");

        var cerSign = await SignCertificateFile(readStream, deviceId);

        DownloadAction downloadAction = new DownloadAction()
        {
            Action = TwinActionType.SingularDownload,
            Description = $"{DateTime.Now.ToShortDateString()} - {DateTime.Now.ToShortTimeString()}",
            Source = Uri.UnescapeDataString(storageUri.Segments.Last()),
            DestinationPath = GetFilePathFromBlobName(Uri.UnescapeDataString(storageUri.Segments.Last())),
            Sign = cerSign
        };
        await _twinDiseredHandler.AddDesiredRecipeAsync(deviceId, SharedConstants.CHANGE_SPEC_DIAGNOSTICS_NAME, downloadAction);
        await UpdateChangeSpecSign(deviceId);
    }
    private async Task UpdateChangeSpecSign(string deviceId)
    {
        var changeSignKey = SharedConstants.CHANGE_SPEC_DIAGNOSTICS_NAME.GetSignKeyByChangeSpec();
        await _changeSpecService.CreateChangeSpecKeySignatureAsync(deviceId, changeSignKey);
    }

    private async Task<string> SignCertificateFile(byte[] data, string deviceId)
    {
        _logger.Info($"Send request to get Sign certificate from keyHolder");

        var signatureFileBytes = await GetCalculateHash(data);

        var cerSign = await _changeSpecService.SendToSignData(signatureFileBytes, deviceId);
        _logger.Info($"Sign certificate from keyHolder: {cerSign}");
        return cerSign;
    }
    // private byte[] CalculateHash(string filePath)
    // {
    //     using (SHA256 sha256 = SHA256.Create())
    //     {
    //         using (FileStream fileStream = File.OpenRead(filePath))
    //         {
    //             byte[] buffer = new byte[fileStream.Length];
    //             int bytesRead;
    //             while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
    //             {
    //                 _sha256Wrapper.TransformBlock(sha256, buffer, 0, bytesRead, null, 0);
    //             }

    //             _sha256Wrapper.TransformFinalBlock(sha256, new byte[0], 0, 0);
    //             return _sha256Wrapper.GetHash(sha256);
    //         }
    //     }
    // }
    private async Task<byte[]> GetCalculateHash(byte[] data)
    {
        using (SHA256 sha256 = _sha256Wrapper.Create())
        {
            try
            {
                _sha256Wrapper.TransformBlock(sha256, data, 0, (int)data.Length, null, 0);
                _sha256Wrapper.TransformFinalBlock(sha256, new byte[0], 0, 0);
                return sha256.Hash;
            }
            catch (Exception ex)
            {
                _logger.Error($"CalculateHashAsync failed.", ex);
                throw;
            }
        }
    }

    private string GetFilePathFromBlobName(string blobName)
    {
        string[] parts = blobName.Split('/');
        var partIndex = parts.ToList().FindIndex(x => x.Contains("_driveroot_"));
        string result = string.Join("\\", parts, partIndex, parts.Length - partIndex);

        var filePath = Regex.Replace(result.Replace("_protocol_", "//:").Replace("_driveroot_", ":").Replace("/", "\\"), "^\\/", "_root_");
        _logger.Info($"GetFilePathFromBlobName, filePath is: {filePath}");
        return filePath;
    }
}