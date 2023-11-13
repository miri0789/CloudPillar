using CloudPillar.Agent.Entities;
using Microsoft.Extensions.Options;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;
public class RunDiagnosticsHandler : IRunDiagnosticsHandler
{
    private const int BYTE_SIZE = 1024;
    private const string FILE_NAME = "diagnosticFile";
    private const string FILE_EXSTENSION = ".txt";
    private string destPath;

    private readonly IFileUploaderHandler _fileUploaderHandler;
    private readonly RunDiagnosticsSettings _runDiagnosticsSettings;
    private readonly ILoggerHandler _logger;

    public RunDiagnosticsHandler(IFileUploaderHandler fileUploaderHandler, IOptions<RunDiagnosticsSettings> runDiagnosticsSettings, ILoggerHandler logger)
    {
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _runDiagnosticsSettings = runDiagnosticsSettings?.Value ?? throw new ArgumentNullException(nameof(runDiagnosticsSettings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        destPath = Path.Combine(_runDiagnosticsSettings.FilePath, FILE_NAME + FILE_EXSTENSION);
    }

    public async Task CreateFileAsync()
    {
        var fileSize = _runDiagnosticsSettings.FleSizeKB * BYTE_SIZE;
        try
        {
            //create random content
            var bytes = new Byte[fileSize];
            new Random().NextBytes(bytes);
            string directoryPath = Path.GetDirectoryName(destPath);

            if (!Directory.Exists(directoryPath))
            {
                // Create the directory if it doesn't exist
                Directory.CreateDirectory(directoryPath);
            }
            using (FileStream fileStream = new FileStream(destPath, FileMode.Create))
            {
                fileStream.SetLength(fileSize);
                await fileStream.WriteAsync(bytes);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"error during creating a file {ex.Message}");
            throw ex;
        }
    }

    public async Task UploadFileAsync(CancellationToken cancellationToken)
    {
        var uploadAction = new UploadAction()
        {
            Action = TwinActionType.SingularUpload,
            Description = "upload file by run diagnostic",
            ActionId = Guid.NewGuid().ToString(),
            Method = FileUploadMethod.Stream,
            FileName = destPath
        };

        var actionToReport = new ActionToReport();
        await _fileUploaderHandler.UploadFilesToBlobStorageAsync(destPath, uploadAction, actionToReport, cancellationToken, true);
    }


    public async Task DownloadFile(CancellationToken cancellationToken)
    {
    }
}