using System.Diagnostics;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Shared.Entities.Services;
using Shared.Entities.Twin;
using Shared.Entities.Utilities;
using Shared.Logger;

namespace CloudPillar.Agent.Handlers;
public class RunDiagnosticsHandler : IRunDiagnosticsHandler
{
    private const int BYTE_SIZE = 1024;
    private readonly IFileUploaderHandler _fileUploaderHandler;
    private readonly RunDiagnosticsSettings _runDiagnosticsSettings;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly ICheckSumService _checkSumService;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly ILoggerHandler _logger;

    public RunDiagnosticsHandler(IFileUploaderHandler fileUploaderHandler, IOptions<RunDiagnosticsSettings> runDiagnosticsSettings, IFileStreamerWrapper fileStreamerWrapper,
    ICheckSumService checkSumService, IDeviceClientWrapper deviceClientWrapper, ILoggerHandler logger)
    {
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _runDiagnosticsSettings = runDiagnosticsSettings?.Value ?? throw new ArgumentNullException(nameof(runDiagnosticsSettings));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
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

    public async Task<string> UploadFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            var actionId = Guid.NewGuid().ToString();
            var uploadAction = new UploadAction()
            {
                Action = TwinActionType.SingularUpload,
                Description = "upload file by run diagnostic",
                Method = FileUploadMethod.Stream,
                FileName = _runDiagnosticsSettings.FilePath,
                ActionId = actionId
            };

            var actionToReport = new ActionToReport(TwinPatchChangeSpec.changeSpecDiagnostics);
            await _fileUploaderHandler.UploadFilesToBlobStorageAsync(_runDiagnosticsSettings.FilePath, uploadAction, actionToReport, cancellationToken, true);
            return actionId;
        }
        catch (Exception ex)
        {
            _logger.Error($"UploadFileAsync error: {ex.Message}");
            throw ex;
        }
    }

    public async Task<StatusType> WaitingForResponse(string actionId)
    {
        var taskCompletion = new TaskCompletionSource<StatusType>();
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        var timeOutTimer = new PeriodicTimer(TimeSpan.FromMinutes(2));
        try
        {

            while (await timer.WaitForNextTickAsync())
            {
                var statusType = await CheckResponse(actionId);
                if (statusType == StatusType.Success)
                {
                    taskCompletion.SetResult(statusType);
                    timer.Dispose();
                }

                // Check if the timeout timer has elapsed
                if (await timeOutTimer.WaitForNextTickAsync())
                {
                    // Dispose of the timer explicitly after 2 minutes
                    throw new Exception($"Timeout");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"WaitForResponse error: {ex.Message}");
            timer.Dispose();
            taskCompletion.SetException(ex);
            throw ex;
        }
        return await taskCompletion.Task;
    }

    private async Task<StatusType> CheckResponse(string actionId)
    {
        var twin = await _deviceClientWrapper.GetTwinAsync(CancellationToken.None);

        var twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();
        var twinReported = JsonConvert.DeserializeObject<TwinReported>(twin.Properties.Reported.ToJson());

        var desiredList = twinDesired.ChangeSpecDiagnostics.Patch.TransitPackage.ToList();
        var reportedList = twinReported.ChangeSpecDiagnostics.Patch.TransitPackage.ToList();

        var indexDesired = desiredList.FindIndex(x => x.ActionId == actionId);
        if (indexDesired == -1 || desiredList.Count() > reportedList.Count())
        {
            return StatusType.Pending;
        }

        var report = reportedList[indexDesired];
        if (report.Status == StatusType.Success)
        {
            return await CompareUploadAndDownloadFiles(((DownloadAction)desiredList[indexDesired]).DestinationPath);
        }
        return StatusType.Pending;
    }

    private async Task<StatusType> CompareUploadAndDownloadFiles(string downloadFilePath)
    {
        var uploadFilePath = _runDiagnosticsSettings.FilePath;
        string uploadChecksum;
        using (FileStream uploadFileStream = File.OpenRead(uploadFilePath))
        {
            uploadChecksum = await _checkSumService.CalculateCheckSumAsync(uploadFileStream);
            _logger.Info($"Upload file check sum: {uploadChecksum}");

        }

        // Calculate checksum for the download file
        string downloadChecksum;
        using (FileStream downloadFileStream = File.OpenRead(downloadFilePath))
        {
            downloadChecksum = await _checkSumService.CalculateCheckSumAsync(downloadFileStream);
            _logger.Info($"download file check sum: {downloadChecksum}");
        }

        // Compare checksums
        var isEqual = uploadChecksum.Equals(downloadChecksum, StringComparison.OrdinalIgnoreCase);
        if (!isEqual)
        {
            throw new Exception("The Upload file is not equal to Download file");
        }
        _logger.Info("RunDiagnostics success: Upload file is equal to Download file");
        return StatusType.Success;
    }
}