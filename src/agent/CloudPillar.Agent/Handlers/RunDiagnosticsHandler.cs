using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.Options;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;
public class RunDiagnosticsHandler : IRunDiagnosticsHandler
{
    private const int BYTE_SIZE = 1024;
    private const string FILE_NAME = "diagnosticFile";
    private const string FILE_EXSTENSION = ".txt";
    private string _basePath = AppDomain.CurrentDomain.BaseDirectory;
    private string _destPath;

    private readonly IFileUploaderHandler _fileUploaderHandler;
    private readonly RunDiagnosticsSettings _runDiagnosticsSettings;
    private readonly ILoggerHandler _logger;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;

    public RunDiagnosticsHandler(IFileUploaderHandler fileUploaderHandler, IOptions<RunDiagnosticsSettings> runDiagnosticsSettings, IFileStreamerWrapper fileStreamerWrapper, ILoggerHandler logger)
    {
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _runDiagnosticsSettings = runDiagnosticsSettings?.Value ?? throw new ArgumentNullException(nameof(runDiagnosticsSettings));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _destPath = Path.Combine(_basePath, FILE_NAME + FILE_EXSTENSION);
    }

    public async Task CreateFileAsync()
    {
        var fileSize = _runDiagnosticsSettings.FleSizeKB * BYTE_SIZE;
        try
        {
            //create random content
            var bytes = new Byte[fileSize];
            new Random().NextBytes(bytes);

            using (FileStream fileStream = _fileStreamerWrapper.CreateStream(_destPath, FileMode.Create))
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
            FileName = _destPath
        };

        var actionToReport = new ActionToReport();
        await _fileUploaderHandler.UploadFilesToBlobStorageAsync(_destPath, uploadAction, actionToReport, cancellationToken);
    }


    public async Task DownloadFile(CancellationToken cancellationToken)
    {
    }
}