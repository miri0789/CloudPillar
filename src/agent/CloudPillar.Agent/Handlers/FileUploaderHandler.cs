using System.Text.RegularExpressions;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using System.IO.Compression;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public class FileUploaderHandler : IFileUploaderHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDeviceClientWrapper _deviceClient;

    public FileUploaderHandler(IServiceProvider serviceProvider, IDeviceClientWrapper deviceClient)
    {
        _serviceProvider = serviceProvider;
        _deviceClient = deviceClient;
    }

    public async Task<ActionToReport> InitFileUploadAsync(UploadAction uploadAction, ActionToReport actionToReport, CancellationToken cancellationToken)
    {
        //for periodicUpload
        var interval = TimeSpan.FromSeconds(uploadAction.Interval > 0 ? uploadAction.Interval : 600);
        var method = !String.IsNullOrEmpty(uploadAction.Method) ? uploadAction.Method : "blob";

        if (uploadAction.FileName != null && uploadAction.Enabled)
        {
            actionToReport.TwinReport = await UploadFilesAsync(uploadAction.FileName, interval, method, cancellationToken);
        }
        return actionToReport;
    }
    public async Task<TwinActionReported> UploadFilesAsync(string filename, TimeSpan interval, string method, CancellationToken cancellationToken)
    {
        TwinActionReported twinAction = new TwinActionReported();

        try
        {
            await UploadFilesToBlobStorageAsync(filename, method, cancellationToken);
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

    private async Task UploadFilesToBlobStorageAsync(string filePathPattern, string method, CancellationToken cancellationToken)
    {
        string? directoryPath = Path.GetDirectoryName(filePathPattern);
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
            var sasUriResponse = await _deviceClient.GetFileUploadSasUriAsync(new FileUploadSasUriRequest
            {
                BlobName = blobname
            });

            IUploadFromStreamHandler uploader = CreateFileUploader(method);
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
                // await blob.UploadFromFileAsync(fullFilePath);
                var fileStream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4 * 1024 * 1024, true);
                fileStream.Position = 0;
                readStream = fileStream;
            }
            var storageUri = sasUriResponse.GetBlobUri();
            // Upload via either memory or file stream set up above
            await uploader.UploadFromStreamAsync(storageUri, readStream, sasUriResponse.CorrelationId, 0,
             async (string correlationId, Exception? exception) =>
            {
                if (exception != null)
                    Console.WriteLine("Error during upload: " + exception.Message);

                await _deviceClient.CompleteFileUploadAsync(new FileUploadCompletionNotification
                {
                    CorrelationId = correlationId,
                    IsSuccess = exception == null
                });
            }, cancellationToken);
        }
    }

    private IUploadFromStreamHandler CreateFileUploader(/*Uri storageUri*/ string uploadMethod)
    {
        switch (uploadMethod)
        {
            case "blob": return _serviceProvider.GetRequiredService<IBlobStorageFileUploaderHandler>();
            case "stream": return _serviceProvider.GetRequiredService<IIoTStreamingFileUploaderHandler>();
            default: throw new ArgumentException("Unsupported upload method", "uploadMethod");
        }
    }

}
