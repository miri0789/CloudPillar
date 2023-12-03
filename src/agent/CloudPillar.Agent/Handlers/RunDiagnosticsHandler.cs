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
            _logger.Info($"File for diagnostics was created");
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

            var actionToReport = new ActionToReport(TwinPatchChangeSpec.ChangeSpecDiagnostics);
            await _fileUploaderHandler.UploadFilesToBlobStorageAsync(_runDiagnosticsSettings.FilePath, uploadAction, actionToReport, cancellationToken, true);
            return actionId;
        }
        catch (Exception ex)
        {
            _logger.Error($"UploadFileAsync error: {ex.Message}");
            throw ex;
        }
    }

    public async Task<TwinActionReported> CheckDownloadStatus(string actionId)
    {
        TwinActionReported reported = new TwinActionReported();
        var taskCompletion = new TaskCompletionSource<TwinActionReported>();
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_runDiagnosticsSettings.PeriodicResponseWaitSeconds));

        Timer timeTaken = new Timer((Object reported) =>
        {
            var state = ((TwinActionReported)reported).Status;
            if (state != StatusType.Success && state != StatusType.Failed)
            {
                taskCompletion.SetException(new TimeoutException($"Something is wrong, no response was received within {_runDiagnosticsSettings.ResponseTimeoutMinutes} minutes"));
            }
        }, reported, _runDiagnosticsSettings.ResponseTimeoutMinutes * 60 * 1000, Timeout.Infinite);

        try
        {
            while (!taskCompletion.Task.IsCompleted && await timer.WaitForNextTickAsync())
            {
                reported = await GetDownloadStatus(actionId);
                _logger.Info($"CheckResponse response is {reported.Status}");

                if (reported.Status == StatusType.Success)
                {
                    var res = await CompareUploadAndDownloadFiles(reported.ResultText);
                    if (!res)
                    {
                        reported.Status = StatusType.Failed;
                        reported.ResultText = "Upload file is not equal to Download file";
                    }
                    taskCompletion.SetResult(reported);
                }
                if (reported.Status == StatusType.Failed)
                {
                    taskCompletion.SetResult(reported);
                }
            }
        }
        catch (Exception ex)
        {
            taskCompletion.SetException(ex);
        }
        finally
        {
            timer.Dispose();
            timeTaken.Dispose();
        }
        return await taskCompletion.Task;
    }

    public async Task DeleteFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _fileUploaderHandler.DeleteFileUploadAsync(_runDiagnosticsSettings.FilePath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"DeleteFileAsync error: {ex.Message}");
            throw ex;
        }
    }
    private async Task<TwinActionReported> GetDownloadStatus(string actionId)
    {

        var twin = await _deviceClientWrapper.GetTwinAsync(CancellationToken.None);

        var twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();
        var twinReported = JsonConvert.DeserializeObject<TwinReported>(twin.Properties.Reported.ToJson());

        var desiredList = twinDesired?.ChangeSpecDiagnostics?.Patch?.TransitPackage?.ToList();
        var reportedList = twinReported?.ChangeSpecDiagnostics?.Patch?.TransitPackage?.ToList();

        var indexDesired = desiredList?.FindIndex(x => x.ActionId == actionId) ?? -1;
        if (indexDesired == -1 || desiredList?.Count() > reportedList?.Count())
        {
            _logger.Info($"No report with actionId {actionId}");
            return new TwinActionReported() { Status = StatusType.Pending };
        }

        var report = reportedList[indexDesired];
        // if (report.Status == StatusType.Success)
        // {
        //     _logger.Info($"File download completed successfully");
        //     return await CompareUploadAndDownloadFiles(((DownloadAction)desiredList[indexDesired]).DestinationPath);
        // }
        if (report.Status == StatusType.Success)
        {
            report.ResultText = ((DownloadAction)desiredList[indexDesired]).DestinationPath;
        }
        return reportedList[indexDesired];
    }

    private async Task<bool> CompareUploadAndDownloadFiles(string downloadFilePath)
    {
        _logger.Info("Start compare upload and download files");

        string uploadChecksum = await GetFileCheckSumAsync(_runDiagnosticsSettings.FilePath);
        string downloadChecksum = await GetFileCheckSumAsync(downloadFilePath);

        var isEqual = uploadChecksum.Equals(downloadChecksum, StringComparison.OrdinalIgnoreCase);
        if (isEqual == true)
        {
            _logger.Info("Upload file is equal to Download file");
        }
        return isEqual;
    }

    private async Task<string> GetFileCheckSumAsync(string filePath)
    {
        string checkSum = string.Empty;
        using (FileStream fileStream = File.OpenRead(filePath))
        {
            checkSum = await _checkSumService.CalculateCheckSumAsync(fileStream);
            _logger.Info($"file check sum: {checkSum}");
        }
        return checkSum;
    }
}