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
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly ILoggerHandler _logger;

    public RunDiagnosticsHandler(IFileUploaderHandler fileUploaderHandler, IOptions<RunDiagnosticsSettings> runDiagnosticsSettings, IFileStreamerWrapper fileStreamerWrapper,
    ICheckSumService checkSumService, IDeviceClientWrapper deviceClientWrapper, ID2CMessengerHandler d2CMessengerHandler, ILoggerHandler logger)
    {
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _runDiagnosticsSettings = runDiagnosticsSettings?.Value ?? throw new ArgumentNullException(nameof(runDiagnosticsSettings));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TwinActionReported> HandleRunDiagnosticsProcess(CancellationToken cancellationToken)
    {
        var diagnosticsFilePath = await CreateFileAsync();
        var actionId = await UploadFileAsync(diagnosticsFilePath, cancellationToken);
        var reported = await CheckDownloadStatus(actionId, diagnosticsFilePath);
        await DeleteFileAsync(diagnosticsFilePath, cancellationToken);
        return reported;
    }

    private async Task<string> CreateFileAsync()
    {
        try
        {
            var diagnosticsFilePath = _fileStreamerWrapper.GetTempFileName();

            //create random content
            var bytes = new Byte[_runDiagnosticsSettings.FleSizBytes];
            new Random().NextBytes(bytes);

            using (FileStream fileStream = _fileStreamerWrapper.CreateStream(diagnosticsFilePath, FileMode.Create))
            {
                fileStream.SetLength(_runDiagnosticsSettings.FleSizBytes);
                await fileStream.WriteAsync(bytes);
            }
            _logger.Info($"File for diagnostics was created");
            return diagnosticsFilePath;
        }
        catch (Exception ex)
        {
            _logger.Error($"CreateFileAsync error: {ex.Message}");
            throw ex;
        }
    }

    private async Task<string> UploadFileAsync(string diagnosticsFilePath, CancellationToken cancellationToken)
    {
        try
        {
            var actionId = Guid.NewGuid().ToString();
            var uploadAction = new UploadAction()
            {
                Action = TwinActionType.SingularUpload,
                Description = "upload file by run diagnostic",
                Method = FileUploadMethod.Stream,
                FileName = diagnosticsFilePath,
                ActionId = actionId
            };

            var actionToReport = new ActionToReport(TwinPatchChangeSpec.ChangeSpecDiagnostics);
            await _fileUploaderHandler.UploadFilesToBlobStorageAsync(uploadAction.FileName, uploadAction, actionToReport, cancellationToken, true);
            return actionId;
        }
        catch (Exception ex)
        {
            _logger.Error($"UploadFileAsync error: {ex.Message}");
            throw ex;
        }
    }

    private async Task<TwinActionReported> CheckDownloadStatus(string actionId, string diagnosticsFilePath)
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
                    var res = await CompareUploadAndDownloadFiles(diagnosticsFilePath, reported.ResultText);
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

    private async Task DeleteFileAsync(string diagnosticsFilePath, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(diagnosticsFilePath);
            await DeleteBlobAsync(diagnosticsFilePath, cancellationToken);
            DeleteTempFile(diagnosticsFilePath);
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
        if (report.Status == StatusType.Success)
        {
            report.ResultText = ((DownloadAction)desiredList[indexDesired]).DestinationPath;
        }
        return reportedList[indexDesired];
    }

    private async Task<bool> CompareUploadAndDownloadFiles(string uploadFilePath, string downloadFilePath)
    {
        _logger.Info("Start compare upload and download files");

        string uploadChecksum = await GetFileCheckSumAsync(uploadFilePath);
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
    private async Task DeleteBlobAsync(string diagnosticsFilePath, CancellationToken cancellationToken)
    {
        var storageUri = await _fileUploaderHandler.GetStorageUriAsync(diagnosticsFilePath);
        await _d2CMessengerHandler.SendDeleteBlobEventAsync(storageUri, cancellationToken);

    }
    private void DeleteTempFile(string filePath)
    {
        if (_fileStreamerWrapper.FileExists(filePath))
        {
            _fileStreamerWrapper.DeleteFile(filePath);
            _logger.Info($"File {filePath} was deleted");
        }
    }
}