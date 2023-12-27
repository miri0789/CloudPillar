using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Azure.Devices.Client.Transport;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public class FileUploaderHandler : IFileUploaderHandler
{
    const int BUFFER_SIZE = 4 * 1024 * 1024;

    private readonly ILoggerHandler _logger;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly IBlobStorageFileUploaderHandler _blobStorageFileUploaderHandler;
    private readonly IStreamingFileUploaderHandler _streamingFileUploaderHandler;
    private readonly ITwinReportHandler _twinReportHandler;

    public FileUploaderHandler(
        IDeviceClientWrapper deviceClientWrapper,
        IFileStreamerWrapper fileStreamerWrapper,
        IBlobStorageFileUploaderHandler blobStorageFileUploaderHandler,
        IStreamingFileUploaderHandler StreamingFileUploaderHandler,
        ITwinReportHandler twinActionsHandler,
        ILoggerHandler logger)
    {
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _blobStorageFileUploaderHandler = blobStorageFileUploaderHandler ?? throw new ArgumentNullException(nameof(blobStorageFileUploaderHandler));
        _streamingFileUploaderHandler = StreamingFileUploaderHandler ?? throw new ArgumentNullException(nameof(StreamingFileUploaderHandler));
        _twinReportHandler = twinActionsHandler ?? throw new ArgumentNullException(nameof(twinActionsHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task FileUploadAsync(UploadAction uploadAction, ActionToReport actionToReport, string fileName, string changeSpecId, CancellationToken twinCancellationToken)
    {
        try
        {
            if (!twinCancellationToken.IsCancellationRequested)
            {
                ArgumentNullException.ThrowIfNull(fileName);
                await UploadFilesToBlobStorageAsync(fileName, uploadAction, actionToReport, changeSpecId, twinCancellationToken);
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
            await _twinReportHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), twinCancellationToken);
        }
    }

    public async Task UploadFilesToBlobStorageAsync(string filePathPattern, UploadAction uploadAction, ActionToReport actionToReport, string changeSpecId, CancellationToken twinCancellationToken, bool isRunDiagnostics = false)
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

            if (!string.IsNullOrEmpty(changeSpecId))
            {
                blobname = $"{changeSpecId}/{blobname}";
            }


            using (Stream readStream = CreateStream(fullFilePath))
            {
                await UploadFileAsync(uploadAction, actionToReport, blobname, readStream, isRunDiagnostics, twinCancellationToken);
            }
        }
    }

    private string BuildBlobName(string fullFilePath)
    {

        string blobname = Regex.Replace(fullFilePath.Replace("//:", "_protocol_").Replace("\\", "/").Replace(":/", "_driveroot_/"), "^\\/", "_root_");

        if (_fileStreamerWrapper.DirectoryExists(fullFilePath))
        {
            blobname += ".zip";
        }
        _logger.Info($"BuildBlobName success name: {blobname}");
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

    private async Task UploadFileAsync(UploadAction uploadAction, ActionToReport actionToReport, string blobname, Stream readStream, bool isRunDiagnostics, CancellationToken twinCancellationToken)
    {
        _logger.Info($"UploadFileAsync");

        FileUploadCompletionNotification notification = new FileUploadCompletionNotification()
        {
            IsSuccess = true
        };

        try
        {
            if (uploadAction.Method == FileUploadMethod.Blob && actionToReport.TwinReport.Status == StatusType.InProgress)
            {
                notification.CorrelationId ??= actionToReport.TwinReport.CorrelationId;
                await _deviceClientWrapper.CompleteFileUploadAsync(notification, twinCancellationToken);
            }
            var sasUriResponse = await _deviceClientWrapper.GetFileUploadSasUriAsync(new FileUploadSasUriRequest
            {
                BlobName = blobname
            });
            var storageUri = _deviceClientWrapper.GetBlobUri(sasUriResponse);
            notification.CorrelationId = sasUriResponse.CorrelationId;
            switch (uploadAction.Method)
            {
                case FileUploadMethod.Blob:
                    _logger.Info($"Upload file: {uploadAction.FileName} by http");

                    await _blobStorageFileUploaderHandler.UploadFromStreamAsync(notification, storageUri, readStream, actionToReport, twinCancellationToken);
                    await _deviceClientWrapper.CompleteFileUploadAsync(notification, twinCancellationToken);
                    _logger.Info($"The file: {uploadAction.FileName} uploaded successfully");

                    break;
                case FileUploadMethod.Stream:
                    await _streamingFileUploaderHandler.UploadFromStreamAsync(notification, actionToReport, readStream, storageUri, sasUriResponse.CorrelationId, twinCancellationToken, isRunDiagnostics);
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
                await _deviceClientWrapper.CompleteFileUploadAsync(notification, twinCancellationToken);
            }

            throw ex;
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
