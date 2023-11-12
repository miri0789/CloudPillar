

using CloudPillar.Agent.Entities;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;
public class RunDiagnosticsHandler : IRunDiagnosticsHandler
{
    private const int FILE_SIZE_KB = 128;
    private const int FILE_SIZE_BYTES = FILE_SIZE_KB * 1024;
    private const string FILE_NAME = "diagnosticFile";
    private const string FILE_EXSTENSION = ".txt";
    private string basePath = AppDomain.CurrentDomain.BaseDirectory;
    private string destPath;

    private readonly IFileUploaderHandler _fileUploaderHandler;
    private readonly ILoggerHandler _logger;

    public RunDiagnosticsHandler(IFileUploaderHandler fileUploaderHandler, ILoggerHandler logger)
    {
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        destPath = Path.Combine(basePath, FILE_NAME + FILE_EXSTENSION);
    }

    public async Task CreateFileAsync()
    {
        try
        {
            //create random content
            var bytes = new Byte[FILE_SIZE_BYTES];
            new Random().NextBytes(bytes);

            using (FileStream fileStream = new FileStream(destPath, FileMode.Create))
            {
                fileStream.SetLength(FILE_SIZE_BYTES);
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