using System.Text.RegularExpressions;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using System.IO.Compression;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public class FileUploaderHandler : IFileUploaderHandler
{
    const int BUFFER_SIZE = 4 * 1024 * 1024;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly IEnvironmentsWrapper _environmentsWrapper;
    private readonly IBlobStorageFileUploaderHandler _blobStorageFileUploaderHandler;
    private readonly IIoTStreamingFileUploaderHandler _ioTStreamingFileUploaderHandler;

    public FileUploaderHandler(
        IDeviceClientWrapper deviceClientWrapper,
        IEnvironmentsWrapper environmentsWrapper,
        IBlobStorageFileUploaderHandler blobStorageFileUploaderHandler,
        IIoTStreamingFileUploaderHandler ioTStreamingFileUploaderHandler)
    {
        ArgumentNullException.ThrowIfNull(deviceClientWrapper);
        ArgumentNullException.ThrowIfNull(environmentsWrapper);
        ArgumentNullException.ThrowIfNull(blobStorageFileUploaderHandler);
        ArgumentNullException.ThrowIfNull(ioTStreamingFileUploaderHandler);

        _deviceClientWrapper = deviceClientWrapper;
        _environmentsWrapper = environmentsWrapper;
        _blobStorageFileUploaderHandler = blobStorageFileUploaderHandler;
        _ioTStreamingFileUploaderHandler = ioTStreamingFileUploaderHandler;
    }

    public async Task<ActionToReport> FileUploadAsync(UploadAction uploadAction, ActionToReport actionToReport, CancellationToken cancellationToken)
    {
        TwinActionReported twinAction = actionToReport.TwinReport;

        try
        {
            if (String.IsNullOrEmpty(uploadAction.FileName))
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
        string directoryPath = Path.GetDirectoryName(filePathPattern) ?? "";
        string searchPattern = Path.GetFileName(filePathPattern);

        // Get a list of all matching files
        string[] files = Directory.GetFiles(directoryPath, searchPattern);
        // Get a list of all matching directories
        string[] directories = Directory.GetDirectories(directoryPath, searchPattern);

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
        string blobname = Regex.Replace(fullFilePath.Replace("//:", "_protocol_").Replace("\\", "/").Replace(":/", "_driveroot_/"), "^\\/", "_root_");
        if (Directory.Exists(fullFilePath))
        {
            blobname += ".zip";
        }
        return blobname;
    }

    private MemoryStream CreateZipArchive(string fullFilePath)
    {
        var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            string baseDir = Path.GetFileName(fullFilePath);
            foreach (string file in Directory.GetFiles(fullFilePath, "*", SearchOption.AllDirectories))
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
            var storageUri = sasUriResponse.GetBlobUri();
            notification.CorrelationId = sasUriResponse.CorrelationId;
            switch (uploadAction.Method)
            {
                case FileUploadMethod.Blob:

                    await _blobStorageFileUploaderHandler.UploadFromStreamAsync(storageUri, readStream, cancellationToken);
                    break;
                case FileUploadMethod.Stream:
                    await _ioTStreamingFileUploaderHandler.UploadFromStreamAsync(readStream, storageUri.AbsolutePath, uploadAction.ActionId, sasUriResponse.CorrelationId);
                    break;
                default:
                    throw new ArgumentException("Unsupported upload method", "uploadMethod");
            }
            await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            notification.IsSuccess = false;
            await _deviceClientWrapper.CompleteFileUploadAsync(notification, cancellationToken);
            throw ex;
        }
    }
}
