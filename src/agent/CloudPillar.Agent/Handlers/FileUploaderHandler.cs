using System.Text.RegularExpressions;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using System.IO.Compression;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;

public class FileUploaderHandler : IFileUploaderHandler
{
    const int BUFFER_SIZE = 4 * 1024 * 1024;

    private readonly ILoggerHandler _logger;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly IBlobStorageFileUploaderHandler _blobStorageFileUploaderHandler;
    private readonly IStreamingFileUploaderHandler _streamingFileUploaderHandler;

    public FileUploaderHandler(
        IDeviceClientWrapper deviceClientWrapper,
        IBlobStorageFileUploaderHandler blobStorageFileUploaderHandler,
        IStreamingFileUploaderHandler StreamingFileUploaderHandler,
        ILoggerHandler logger)
    {
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _blobStorageFileUploaderHandler = blobStorageFileUploaderHandler ?? throw new ArgumentNullException(nameof(blobStorageFileUploaderHandler));
        _streamingFileUploaderHandler = StreamingFileUploaderHandler ?? throw new ArgumentNullException(nameof(StreamingFileUploaderHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ActionToReport> FileUploadAsync(UploadAction uploadAction, ActionToReport actionToReport, CancellationToken cancellationToken)
    {
        _logger.Info($"FileUploadAsync");
        TwinActionReported twinAction = actionToReport.TwinReport;

        try
        {
            if (string.IsNullOrWhiteSpace(uploadAction.FileName))
            {
                throw new ArgumentException("No file to upload");
            }
            if (uploadAction.Enabled)
            {
                await UploadFilesToBlobStorageAsync(uploadAction.FileName, uploadAction, cancellationToken);
                twinAction.Status = StatusType.Success;
                twinAction.ResultCode = ResultCode.Done.ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file '{uploadAction.FileName}': {ex.Message}");
            twinAction.Status = StatusType.Failed;
            twinAction.ResultText = ex.Message;
            twinAction.ResultCode = ex.GetType().Name;
        }

        return actionToReport;
    }

    private async Task UploadFilesToBlobStorageAsync(string filePathPattern, UploadAction uploadAction, CancellationToken cancellationToken)
    {
        _logger.Info($"UploadFilesToBlobStorageAsync");

        string directoryPath = Path.GetDirectoryName(filePathPattern) ?? "";
        string searchPattern = Path.GetFileName(filePathPattern);

        // Get a list of all matching files
        string[] files = Directory.GetFiles(directoryPath, searchPattern);
        // Get a list of all matching directories
        string[] directories = Directory.GetDirectories(directoryPath, searchPattern);

        string[] filesToUpload = files.Concat(directories).ToArray();

        if (filesToUpload.Length == 0)
        {
            throw new ArgumentNullException($"The file {filePathPattern} not found");
        }
        // Upload each file
        foreach (string fullFilePath in files.Concat(directories))
        {
            string blobname = BuildBlobName(fullFilePath);

            using (Stream readStream = CreateStream(fullFilePath))
            {
                await UploadFileAsync(uploadAction, blobname, readStream, cancellationToken);
            }
        }
    }

    private string BuildBlobName(string fullFilePath)
    {
        _logger.Info($"BuildBlobName");

        string blobname = Regex.Replace(fullFilePath.Replace("//:", "_protocol_").Replace("\\", "/").Replace(":/", "_driveroot_/"), "^\\/", "_root_");
        if (Directory.Exists(fullFilePath))
        {
            blobname += ".zip";
        }
        return blobname;
    }

    private MemoryStream CreateZipArchive(string fullFilePath)
    {
        _logger.Info($"CreateZipArchive");

        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            string baseDir = Path.GetFileName(fullFilePath);
            string[] filesDir = Directory.GetFiles(fullFilePath, "*", SearchOption.AllDirectories);
            if (filesDir.Length == 0)
            {
                throw new ArgumentNullException($"Directory {baseDir} is empty");
            }
            foreach (string file in filesDir)
            {
                string relativePath = Path.Combine(baseDir, file.Substring(fullFilePath.Length + 1));
                archive.CreateEntryFromFile(file, relativePath);
            }
        }

        memoryStream.Position = 0;
        return memoryStream; // Will read from memory where the zip file was formed
    }

    private Stream CreateStream(string fullFilePath)
    {
        _logger.Info($"CreateStream");

        Stream readStream;

        // Check if the path is a directory
        if (Directory.Exists(fullFilePath))
        {
            readStream = CreateZipArchive(fullFilePath); // Will read from memory where the zip file was formed
        }
        else
        {
            var fileStream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, true)
            {
                Position = 0
            };
            readStream = fileStream;
        }
        return readStream;
    }

    private async Task UploadFileAsync(UploadAction uploadAction, string blobname, Stream readStream, CancellationToken cancellationToken)
    {
        _logger.Info($"UploadFileAsync");

        FileUploadCompletionNotification notification = new FileUploadCompletionNotification()
        {
            IsSuccess = true
        };
        try
        {
            var sasUriResponse = await _deviceClientWrapper.GetFileUploadSasUriAsync(new FileUploadSasUriRequest
            {
                BlobName = blobname
            });
            var storageUri = await _deviceClientWrapper.GetBlobUriAsync(sasUriResponse);
            notification.CorrelationId = sasUriResponse.CorrelationId;
            switch (uploadAction.Method)
            {
                case FileUploadMethod.Blob:
                    _logger.Info($"Upload file: {uploadAction.FileName} by http");

                    await _blobStorageFileUploaderHandler.UploadFromStreamAsync(storageUri, readStream, cancellationToken);
                    await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
                    _logger.Info($"The file: {uploadAction.FileName} uploaded successfully");

                    break;
                case FileUploadMethod.Stream:
                    await _streamingFileUploaderHandler.UploadFromStreamAsync(readStream, storageUri, uploadAction.ActionId, sasUriResponse.CorrelationId,cancellationToken);
                    break;
                default:
                    throw new ArgumentException("Unsupported upload method", "uploadMethod");
            }
        }
        catch (Exception ex)
        {
            notification.IsSuccess = false;
            await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
            throw ex;
        }
    }
}
