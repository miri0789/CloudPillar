using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Shared.Entities.Services;
using Shared.Entities.Twin;
using Shared.Entities.Utilities;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers.Interfaces;

namespace CloudPillar.Agent.Handlers;
public class RunDiagnosticsHandler : IRunDiagnosticsHandler
{
    private readonly IFileUploaderHandler _fileUploaderHandler;
    private readonly RunDiagnosticsSettings _runDiagnosticsSettings;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly ICheckSumService _checkSumService;
    private readonly IDeviceClientWrapper _deviceClientWrapper;
    private readonly IGuidWrapper _guidWrapper;
    private readonly ILoggerHandler _logger;

    private const string DIAGNOSTICS_EXTENSION = ".tmp";

    public RunDiagnosticsHandler(IFileUploaderHandler fileUploaderHandler, IOptions<RunDiagnosticsSettings> runDiagnosticsSettings, IFileStreamerWrapper fileStreamerWrapper,
    ICheckSumService checkSumService, IDeviceClientWrapper deviceClientWrapper, IGuidWrapper guidWrapper, ILoggerHandler logger)
    {
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _runDiagnosticsSettings = runDiagnosticsSettings?.Value ?? throw new ArgumentNullException(nameof(runDiagnosticsSettings));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _deviceClientWrapper = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _guidWrapper = guidWrapper ?? throw new ArgumentNullException(nameof(guidWrapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TwinActionReported> HandleRunDiagnosticsProcess(CancellationToken cancellationToken)
    {
        var diagnosticsFilePath = await CreateFileAsync();
        var uploadCheckSum = await UploadFileAsync(diagnosticsFilePath, cancellationToken);

        var fileName = _fileStreamerWrapper.GetFileName(diagnosticsFilePath);
        var reported = await CheckDownloadStatus(fileName);
        if (reported.Status == StatusType.Success)
        {
            var equal = await CompareUploadAndDownloadFiles(uploadCheckSum, diagnosticsFilePath);
            if (!equal)
            {
                reported.Status = StatusType.Failed;
                reported.ResultText = "Upload file is not equal to Download file";
            }
        }
        DeleteTempFile(diagnosticsFilePath);
        return reported;
    }

    private async Task<string> CreateFileAsync()
    {
        try
        {
            var bytes = new Byte[_runDiagnosticsSettings.FileSizeBytes];
            new Random().NextBytes(bytes);

            string diagnosticsFilePath = Path.Combine(_fileStreamerWrapper.GetTempPath(), $"{_guidWrapper.CreateNewGuid()}{DIAGNOSTICS_EXTENSION}");
            using (FileStream fileStream = _fileStreamerWrapper.CreateStream(diagnosticsFilePath, FileMode.OpenOrCreate))
            {
                _fileStreamerWrapper.SetLength(fileStream, _runDiagnosticsSettings.FileSizeBytes);
                await _fileStreamerWrapper.WriteAsync(fileStream, bytes);
            }
            _logger.Info($"File for diagnostics was created");
            return diagnosticsFilePath;
        }
        catch (Exception ex)
        {
            var err = $"CreateFileAsync error: {ex.Message}";
            _logger.Error(err);
            throw new Exception(err);
        }
    }

    private async Task<string> UploadFileAsync(string diagnosticsFilePath, CancellationToken cancellationToken)
    {
        try
        {
            var uploadAction = new UploadAction()
            {
                Action = TwinActionType.SingularUpload,
                Description = "upload file by run diagnostic",
                Method = FileUploadMethod.Stream,
                FileName = diagnosticsFilePath
            };

            var actionToReport = new ActionToReport(TwinPatchChangeSpec.ChangeSpecDiagnostics);
            const string DIAGNOSTICS_ID = "diagnostics";
            await _fileUploaderHandler.UploadFilesToBlobStorageAsync(uploadAction.FileName, uploadAction, actionToReport, DIAGNOSTICS_ID, cancellationToken, true);
            var checkSum = await GetFileCheckSumAsync(diagnosticsFilePath);
            DeleteTempFile(diagnosticsFilePath);
            return checkSum;
        }
        catch (Exception ex)
        {
            var err = $"UploadFileAsync error: {ex.Message}";
            _logger.Error(err, ex);
            throw new Exception(err);
        }
    }

    private async Task<TwinActionReported> CheckDownloadStatus(string fileName)
    {
        TwinActionReported? reported = new TwinActionReported();
        var taskCompletion = new TaskCompletionSource<TwinActionReported>();
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_runDiagnosticsSettings.PeriodicResponseWaitSeconds));

        Timer timeTaken = new Timer(reported =>
        {
            var state = ((TwinActionReported?)reported)?.Status;
            if (state != StatusType.Success && state != StatusType.Failed)
            {
                taskCompletion.SetException(new TimeoutException($"Something is wrong, no response was received within {_runDiagnosticsSettings.ResponseTimeoutMinutes} minutes"));
            }
        }, reported, _runDiagnosticsSettings.ResponseTimeoutMinutes * 60 * 1000, Timeout.Infinite);

        try
        {
            while (!taskCompletion.Task.IsCompleted && await timer.WaitForNextTickAsync())
            {
                reported = await GetDownloadStatus(fileName);
                _logger.Info($"CheckResponse response is {reported?.Status}");

                if (reported.Status == StatusType.Success || reported.Status == StatusType.Failed)
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

    private async Task<TwinActionReported> GetDownloadStatus(string fileName)
    {

        var twin = await _deviceClientWrapper.GetTwinAsync(CancellationToken.None);

        var twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();
        var twinReported = JsonConvert.DeserializeObject<TwinReported>(twin.Properties.Reported.ToJson());

        var desiredList = twinDesired?.ChangeSpecDiagnostics?.Patch?.TransitPackage?.ToList();
        var reportedList = twinReported?.ChangeSpecDiagnostics?.Patch?.TransitPackage?.ToList();

        var indexDesired = desiredList?.FindIndex(x => x is DownloadAction && Path.GetFileName(((DownloadAction)x).Source) == fileName) ?? -1;
        if (indexDesired == -1 || desiredList?.Count() > reportedList?.Count())
        {
            _logger.Info($"No report with fileName {fileName}");
            return new TwinActionReported() { Status = StatusType.Pending };
        }

        return reportedList[indexDesired];
    }

    private async Task<bool> CompareUploadAndDownloadFiles(string uploadCheckSum, string downloadFilePath)
    {
        _logger.Info("Start compare upload and download files");

        string downloadChecksum = await GetFileCheckSumAsync(downloadFilePath);

        var isEqual = uploadCheckSum?.Equals(downloadChecksum, StringComparison.OrdinalIgnoreCase) ?? false;

        if (isEqual == true)
        {
            _logger.Info("Upload file is equal to Download file");
        }
        return isEqual;
    }

    private async Task<string> GetFileCheckSumAsync(string filePath)
    {
        string checkSum = string.Empty;
        using (FileStream fileStream = _fileStreamerWrapper.OpenRead(filePath))
        {
            checkSum = await _checkSumService.CalculateCheckSumAsync(fileStream);
            _logger.Info($"file check sum: {checkSum}");
        }
        return checkSum;
    }

    private void DeleteTempFile(string filePath)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        if (_fileStreamerWrapper.FileExists(filePath))
        {
            _fileStreamerWrapper.DeleteFile(filePath);
            _logger.Info($"File {filePath} was deleted");
        }
    }
}