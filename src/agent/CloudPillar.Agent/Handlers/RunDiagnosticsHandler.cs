using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.Options;
using Shared.Entities.Services;
using Shared.Entities.Twin;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;
public class RunDiagnosticsHandler : IRunDiagnosticsHandler
{
    private const int BYTE_SIZE = 1024;
    private readonly IFileUploaderHandler _fileUploaderHandler;
    private readonly RunDiagnosticsSettings _runDiagnosticsSettings;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly ICheckSumService _checkSumService;
    private readonly ILoggerHandler _logger;

    public RunDiagnosticsHandler(IFileUploaderHandler fileUploaderHandler, IOptions<RunDiagnosticsSettings> runDiagnosticsSettings, IFileStreamerWrapper fileStreamerWrapper, ICheckSumService checkSumService, ILoggerHandler logger)
    {
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _runDiagnosticsSettings = runDiagnosticsSettings?.Value ?? throw new ArgumentNullException(nameof(runDiagnosticsSettings));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task CreateFileAsync()
    {
        try
        {
            if (_fileStreamerWrapper.FileExists(_runDiagnosticsSettings.FilePath))
            {
                _logger.Info($"File {_runDiagnosticsSettings.FilePath} exists, continue to uploading this file");
                return;
            }
            //create random content
            var bytes = new Byte[_runDiagnosticsSettings.FleSizBytes];
            new Random().NextBytes(bytes);

            string directoryPath = Path.GetDirectoryName(_runDiagnosticsSettings.FilePath);
            if (!_fileStreamerWrapper.DirectoryExists(directoryPath))
            {
                _fileStreamerWrapper.CreateDirectory(directoryPath);
                _logger.Info($"{directoryPath} was created");
            }
            using (FileStream fileStream = _fileStreamerWrapper.CreateStream(_runDiagnosticsSettings.FilePath, FileMode.Create))
            {
                fileStream.SetLength(_runDiagnosticsSettings.FleSizBytes);
                await fileStream.WriteAsync(bytes);
            }
            _logger.Info($"File for diagnostics was crested");
        }
        catch (Exception ex)
        {
            _logger.Error($"CreateFileAsync error: {ex.Message}");
            throw ex;
        }
    }

    public async Task UploadFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            var uploadAction = new UploadAction()
            {
                Action = TwinActionType.SingularUpload,
                Description = "upload file by run diagnostic",
                Method = FileUploadMethod.Stream,
                FileName = _runDiagnosticsSettings.FilePath
            };

            var actionToReport = new ActionToReport(TwinPatchChangeSpec.changeSpecDiagnostics);
            await _fileUploaderHandler.UploadFilesToBlobStorageAsync(_runDiagnosticsSettings.FilePath, uploadAction, actionToReport, cancellationToken, true);
            // await _fileUploaderHandler.FileUploadAsync(uploadAction, actionToReport, _runDiagnosticsSettings.FilePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"UploadFileAsync error: {ex.Message}");
            throw ex;
        }
    }

    public async Task<bool> CompareUploadAndDownloadFiles(string downloadFilePath)
    {
        var uploadFilePath = _runDiagnosticsSettings.FilePath;
        string uploadChecksum;
        using (FileStream uploadFileStream = File.OpenRead(uploadFilePath))
        {
            uploadChecksum = await _checkSumService.CalculateCheckSumAsync(uploadFileStream);
        }

        // Calculate checksum for the download file
        string downloadChecksum;
        using (FileStream downloadFileStream = File.OpenRead(downloadFilePath))
        {
            downloadChecksum = await _checkSumService.CalculateCheckSumAsync(downloadFileStream);
        }

        // Compare checksums
        var isEqual = uploadChecksum.Equals(downloadChecksum, StringComparison.OrdinalIgnoreCase);
        if(!isEqual){
            throw new Exception("Run Diagnostics error: The Download file is not equal for upload file");
        }
        return isEqual;
    }
}