using System.Text.RegularExpressions;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using System.IO.Compression;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public class FileUploaderHandler : IFileUploaderHandler
{
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

    public async Task<ActionToReport> InitFileUploadAsync(UploadAction uploadAction, ActionToReport actionToReport, CancellationToken cancellationToken)
    {
        //for periodicUpload
        var interval = TimeSpan.FromSeconds(uploadAction.Interval > 0 ? uploadAction.Interval : Convert.ToInt32(_environmentsWrapper.periodicUploadInterval));
        var uploadMethod = uploadAction.Method != null ? uploadAction.Method : FileUploadMethod.Blob;

        if (uploadAction.FileName != null && uploadAction.Enabled)
        {
            actionToReport.TwinReport = await UploadFilesAsync(uploadAction.FileName, interval, uploadMethod, cancellationToken);
        }
        return actionToReport;
    }
    public async Task<TwinActionReported> UploadFilesAsync(string filename, TimeSpan interval, FileUploadMethod uploadMethod, CancellationToken cancellationToken)
    {
        TwinActionReported twinAction = new TwinActionReported();

        try
        {
            await UploadFilesToBlobStorageAsync(filename, uploadMethod, cancellationToken);
            twinAction.Status = StatusType.Success;
            twinAction.ResultText = "Uploaded successfully";
            twinAction.ResultCode = ResultCode.Done.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error uploading file '{filename}': {ex.Message}");

            twinAction.Status = StatusType.Failed;
            twinAction.ResultText = ex.Message;
            twinAction.ResultCode = ex.GetType().Name;
        }

        return twinAction;
    }

    private async Task UploadFilesToBlobStorageAsync(string filePathPattern, FileUploadMethod uploadMethod, CancellationToken cancellationToken)
    {
        string directoryPath = Path.GetDirectoryName(filePathPattern) ?? "";
        string searchPattern = Path.GetFileName(filePathPattern);

        // Get a list of all matching files
        string[] files = Directory.GetFiles(directoryPath ?? "", searchPattern);
        // Get a list of all matching directories
        string[] directories = Directory.GetDirectories(directoryPath ?? "", searchPattern);

        // Upload each file
        foreach (string fullFilePath in files.Concat(directories))
        {
            string blobname = Regex.Replace(fullFilePath.Replace("//:", "_protocol_").Replace("\\", "/").Replace(":/", "_driveroot_/"), "^\\/", "_root_");

            // Check if the path is a directory
            if (Directory.Exists(fullFilePath))
            {
                blobname += ".zip";
            }
            var sasUriResponse = await _deviceClientWrapper.GetFileUploadSasUriAsync(new FileUploadSasUriRequest
            {
                BlobName = blobname
            });

            Stream? readStream = null;
            if (Directory.Exists(fullFilePath))
            {
                // Create a zip in memory
                using (var memoryStream = new MemoryStream())
                {
                    using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                    {
                        string baseDir = Path.GetFileName(fullFilePath);
                        foreach (string file in Directory.GetFiles(fullFilePath, "*", SearchOption.AllDirectories))
                        {
                            string relativePath = baseDir + file.Substring(fullFilePath.Length);
                            archive.CreateEntryFromFile(file, relativePath);
                        }
                    }

                    memoryStream.Position = 0;
                    readStream = memoryStream; // Will read from memory where the zip file was formed
                }
            }
            else
            {
                var fileStream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4 * 1024 * 1024, true)
                {
                    Position = 0
                };
                readStream = fileStream;
            }
            var storageUri = sasUriResponse.GetBlobUri();
            try
            {
                switch (uploadMethod)
                {
                    case FileUploadMethod.Blob:
                        await _blobStorageFileUploaderHandler.UploadFromStreamAsync(storageUri, readStream, sasUriResponse.CorrelationId, cancellationToken);
                        break;
                    case FileUploadMethod.Stream:
                        await _ioTStreamingFileUploaderHandler.UploadFromStreamAsync(storageUri, readStream, sasUriResponse.CorrelationId, 0, cancellationToken);
                        break;
                    default:
                        throw new ArgumentException("Unsupported upload method", "uploadMethod");
                }
                await _deviceClientWrapper.CompleteFileUploadAsync(sasUriResponse.CorrelationId, true);
            }
            catch (Exception ex)
            {
                await _deviceClientWrapper.CompleteFileUploadAsync(sasUriResponse.CorrelationId, false);
                throw ex;
            }
        }
    }
}
