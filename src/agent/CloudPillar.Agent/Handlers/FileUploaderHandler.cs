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
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly IBlobStorageFileUploaderHandler _blobStorageFileUploaderHandler;
    private readonly IStreamingFileUploaderHandler _streamingFileUploaderHandler;
    private readonly ITwinActionsHandler _twinActionsHandler;

    public FileUploaderHandler(
        IDeviceClientWrapper deviceClientWrapper,
        IFileStreamerWrapper fileStreamerWrapper,
        IBlobStorageFileUploaderHandler blobStorageFileUploaderHandler,
        IStreamingFileUploaderHandler StreamingFileUploaderHandler,
        ITwinActionsHandler twinActionsHandler,
        ILoggerHandler logger)
    {
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _blobStorageFileUploaderHandler = blobStorageFileUploaderHandler ?? throw new ArgumentNullException(nameof(blobStorageFileUploaderHandler));
        _streamingFileUploaderHandler = StreamingFileUploaderHandler ?? throw new ArgumentNullException(nameof(StreamingFileUploaderHandler));
        _twinActionsHandler = twinActionsHandler ?? throw new ArgumentNullException(nameof(twinActionsHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task FileUploadAsync(UploadAction uploadAction, ActionToReport actionToReport, string fileName, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("No file to upload");
            }
            if (uploadAction.Enabled)
            {
                await UploadFilesToBlobStorageAsync(fileName, uploadAction, actionToReport, cancellationToken);
                SetReportProperties(actionToReport, StatusType.Success, ResultCode.Done.ToString());
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error uploading file '{fileName}': {ex.Message}");
            SetReportProperties(actionToReport, StatusType.Failed, ex.Message, ex.GetType().Name);
        }
        finally
        {
            await _twinActionsHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);
        }
    }

    public async Task UploadFilesToBlobStorageAsync(string filePathPattern, UploadAction uploadAction, ActionToReport actionToReport, CancellationToken cancellationToken)
    {
        _logger.Info($"UploadFilesToBlobStorageAsync");

        string directoryPath = _fileStreamerWrapper.GetDirectoryName(filePathPattern) ?? "";
        string searchPattern = _fileStreamerWrapper.GetFileName(filePathPattern);

        // Get a list of all matching files
        string[] files = _fileStreamerWrapper.GetFiles(directoryPath, searchPattern);
        // Get a list of all matching directories
        string[] directories = _fileStreamerWrapper.GetDirectories(directoryPath, searchPattern);

        string[] filesToUpload = _fileStreamerWrapper.Concat(files, directories);

        if (filesToUpload.Length == 0)
        {
            throw new ArgumentNullException($"The file {filePathPattern} not found");
        }
        // Upload each file
        foreach (string fullFilePath in _fileStreamerWrapper.Concat(files, directories))
        {
            string blobname = BuildBlobName(fullFilePath);

            using (Stream readStream = CreateStream(fullFilePath))
            {
                await UploadFileAsync(uploadAction, actionToReport, blobname, readStream, cancellationToken);
            }
        }
    }

    private string BuildBlobName(string fullFilePath)
    {
        _logger.Info($"BuildBlobName");

        string blobname = Regex.Replace(fullFilePath.Replace("//:", "_protocol_").Replace("\\", "/").Replace(":/", "_driveroot_/"), "^\\/", "_root_");

        if (_fileStreamerWrapper.DirectoryExists(fullFilePath))
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
            string[] filesDir = _fileStreamerWrapper.GetFiles(fullFilePath, "*", SearchOption.AllDirectories);
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
        if (_fileStreamerWrapper.DirectoryExists(fullFilePath))
        {
            readStream = CreateZipArchive(fullFilePath); // Will read from memory where the zip file was formed
        }
        else
        {
            readStream = _fileStreamerWrapper.CreateStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, true);
            ArgumentNullException.ThrowIfNull(readStream);

            readStream.Position = 0;
        }
        return readStream;
    }

    private async Task UploadFileAsync(UploadAction uploadAction, ActionToReport actionToReport, string blobname, Stream readStream, CancellationToken cancellationToken)
    {
        _logger.Info($"UploadFileAsync");

        FileUploadCompletionNotification notification = new FileUploadCompletionNotification()
        {
            IsSuccess = true
        };

        try
        {
            if (actionToReport.TwinReport.Progress > 0)
            {
                notification.CorrelationId = actionToReport.TwinReport.CorrelationId;
                await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
            }
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

                    await _blobStorageFileUploaderHandler.UploadFromStreamAsync(notification, storageUri, readStream, actionToReport, cancellationToken);
                    await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
                    _logger.Info($"The file: {uploadAction.FileName} uploaded successfully");

                    break;
                case FileUploadMethod.Stream:
                    await _streamingFileUploaderHandler.UploadFromStreamAsync(notification, actionToReport, readStream, storageUri, uploadAction.ActionId, cancellationToken);
                    break;
                default:
                    throw new ArgumentException("Unsupported upload method", "uploadMethod");
            }
        }
        catch (Exception ex)
        {

            notification.IsSuccess = false;
            notification.CorrelationId ??= actionToReport.TwinReport.CorrelationId;

            if (!string.IsNullOrEmpty(notification.CorrelationId))
            {
                await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
            }

            throw new Exception(ex.Message);
        }
    }

    private void SetReportProperties(ActionToReport actionToReport, StatusType status, string resultCode)
    {
        actionToReport.TwinReport.Status = status;
        actionToReport.TwinReport.ResultText = resultCode;
    }
    private void SetReportProperties(ActionToReport actionToReport, StatusType status, string resultCode, string resultText)
    {
        SetReportProperties(actionToReport, status, resultCode);
        actionToReport.TwinReport.ResultCode = resultText;
    }
}
