using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Services;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public class PeriodicUploaderHandler : IPeriodicUploaderHandler
{
    private readonly ILoggerHandler _logger;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly IFileUploaderHandler _fileUploaderHandler;
    private readonly ICheckSumService _checkSumService;
    private readonly ITwinReportHandler _twinReportHandler;

    public PeriodicUploaderHandler(ILoggerHandler logger,
                                   IFileStreamerWrapper fileStreamerWrapper,
                                   IFileUploaderHandler fileUploaderHandler,
                                   ICheckSumService checkSumService,
                                   ITwinReportHandler twinReportHandler)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _twinReportHandler = twinReportHandler ?? throw new ArgumentNullException(nameof(twinReportHandler));
    }


    public async Task UploadAsync(ActionToReport actionToReport, string changeSpecId, CancellationToken cancellationToken)
    {
        var uploadAction = (PeriodicUploadAction)actionToReport.TwinAction;
        _logger.Info($"UploadPeriodicAsync: start dir: {uploadAction.DirName}");
        try
        {
            if (!string.IsNullOrWhiteSpace(uploadAction.DirName) && !_fileStreamerWrapper.DirectoryExists(uploadAction.DirName))
            {
                throw new ArgumentException($"Directory {uploadAction.DirName} does not exist");
            }
            if (!string.IsNullOrWhiteSpace(uploadAction.FileName) && !_fileStreamerWrapper.FileExists(uploadAction.FileName))
            {
                throw new ArgumentException($"File {uploadAction.FileName} does not exist");
            }
            var isDirectory = !string.IsNullOrWhiteSpace(uploadAction.DirName);
            if (isDirectory)
            {
                actionToReport.TwinReport.PeriodicReported ??= new Dictionary<string, TwinActionReported>();
            }
            _twinReportHandler.SetReportProperties(actionToReport, StatusType.InProgress);
            await _twinReportHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);

            string[] files = isDirectory ? _fileStreamerWrapper.GetFiles(uploadAction.DirName, "*", SearchOption.AllDirectories) :
                new string[] { uploadAction.FileName };

            if (files.Count() == 0)
            {
                _logger.Info($"UploadPeriodicAsync: {uploadAction.DirName} is empty");
            }
            foreach (var file in files)
            {
                if (isDirectory)
                {
                    var key = _twinReportHandler.GetPeriodicReportedKey(uploadAction, file);
                    if (!actionToReport.TwinReport.PeriodicReported.ContainsKey(key))
                    {
                        actionToReport.TwinReport.PeriodicReported.Add(key, new TwinActionReported() { Status = StatusType.Pending });
                    }
                }

                var report = _twinReportHandler.GetActionToReport(actionToReport, file);
                if (string.IsNullOrWhiteSpace(report.CheckSum) || await GetFileCheckSumAsync(file) != report.CheckSum)
                {
                    report.Progress = null;
                    report.CorrelationId = report.CheckSum = null;
                    await _fileUploaderHandler.FileUploadAsync(actionToReport, uploadAction.Method, file, changeSpecId, cancellationToken);
                    if (!isDirectory && report.Status == StatusType.Failed)
                    {
                        throw new Exception(report.ResultText);
                    }
                }
                else
                {
                    _logger.Info($"UploadPeriodicAsync: {file} is up to date");
                }
            }

            _twinReportHandler.SetReportProperties(actionToReport, uploadAction.Interval > 0 ? StatusType.Idle : StatusType.Success);
        }
        catch (Exception ex)
        {
            _logger.Error($"Periodic upload error DirName: '{uploadAction.DirName}', FileName: '{uploadAction.FileName}': {ex.Message}");
            _twinReportHandler.SetReportProperties(actionToReport, uploadAction.Interval > 0 ? StatusType.Idle : StatusType.Failed, ex.GetType().Name, ex.Message);
        }
        finally
        {
            if (uploadAction.Interval > 0)
            {
                Task.Run(async () => IdlePeriodicAsync(actionToReport, changeSpecId, cancellationToken));
            }
            await _twinReportHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);
        }
    }

    private async Task<string> GetFileCheckSumAsync(string file)
    {
        using (Stream readStream = _fileStreamerWrapper.CreateStream(file, FileMode.Open, FileAccess.Read))
        {
            return await _checkSumService.CalculateCheckSumAsync(readStream);
        }
    }

    private async Task IdlePeriodicAsync(ActionToReport actionToReport, string changeSpecId, CancellationToken cancellationToken)
    {
        var uploadAction = (PeriodicUploadAction)actionToReport.TwinAction;
        _logger.Info($"Upload periodic of directory {uploadAction.DirName} idle");
        await Task.Delay(TimeSpan.FromSeconds((double)uploadAction.Interval), cancellationToken);
        if (!cancellationToken.IsCancellationRequested)
        {
            await UploadAsync(actionToReport, changeSpecId, cancellationToken);
        }
    }
}
