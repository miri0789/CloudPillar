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

            string directoryPath = _fileStreamerWrapper.GetDirectoryName(_runDiagnosticsSettings.FilePath);
            if (!_fileStreamerWrapper.DirectoryExists(directoryPath))
            {
                _fileStreamerWrapper.CreateDirectory(directoryPath);
                _logger.Info($"{directoryPath} was created");
            }
            using (FileStream fileStream = _fileStreamerWrapper.CreateStream(_runDiagnosticsSettings.FilePath, FileMode.Create))
            {
                _fileStreamerWrapper.SetLength(fileStream, _runDiagnosticsSettings.FleSizBytes);
                await _fileStreamerWrapper.WriteAsync(fileStream, bytes);
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

    public async Task<StatusType> WaitingForResponseAsync(string actionId)
    {
        StatusType statusType = StatusType.Pending;
        var taskCompletion = new TaskCompletionSource<StatusType>();
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_runDiagnosticsSettings.PeriodicResponseWaitSeconds));

        Timer timeTaken = new Timer((Object state) =>
        {
            if ((StatusType)state != StatusType.Success && (StatusType)state != StatusType.Failed)
            {
                taskCompletion.SetException(new TimeoutException($"Something is wrong, no response was received within {_runDiagnosticsSettings.ResponseTimeoutMinutes} minutes"));
            }
        }, statusType, _runDiagnosticsSettings.ResponseTimeoutMinutes * 60 * 1000, Timeout.Infinite);

        try
        {
            while (await timer.WaitForNextTickAsync() && !taskCompletion.Task.IsCompleted)
            {
                statusType = await CheckResponse(actionId);
                _logger.Info($"CheckResponse response is {statusType}");
                if (statusType == StatusType.Success)
                {
                    timer.Dispose();
                    timeTaken.Dispose();
                    taskCompletion.SetResult(statusType);
                }
            }
        }
        catch (Exception ex)
        {
            timer.Dispose();
            taskCompletion.SetException(ex);
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
            _logger.Info($"No report with actionId {actionId}");
            return StatusType.Pending;
        }

        var report = reportedList[indexDesired];
        if (report.Status == StatusType.Success)
        {
            _logger.Info($"File download completed successfully");
            return await CompareUploadAndDownloadFiles(((DownloadAction)desiredList[indexDesired]).DestinationPath);
        }
        return StatusType.Pending;
    }

    private async Task<StatusType> CompareUploadAndDownloadFiles(string downloadFilePath)
    {
        _logger.Info("Start compare upload and download files");

        string uploadChecksum = await GetFileCheckSumAsync(_runDiagnosticsSettings.FilePath);
        string downloadChecksum = await GetFileCheckSumAsync(downloadFilePath);

        var isEqual = uploadChecksum.Equals(downloadChecksum, StringComparison.OrdinalIgnoreCase);
        if (!isEqual)
        {
            throw new Exception("The Upload file is not equal to Download file");
        }
        _logger.Info("Upload file is equal to Download file");
        return StatusType.Success;
    }

    private async Task<string> GetFileCheckSumAsync(string filePath)
    {
        string checkSum;
        using (FileStream fileStream = _fileStreamerWrapper.OpenRead(filePath))
        {
            checkSum = await _checkSumService.CalculateCheckSumAsync(fileStream);
            _logger.Info($"file check sum: {checkSum}");
        }
        return checkSum;
    }
}