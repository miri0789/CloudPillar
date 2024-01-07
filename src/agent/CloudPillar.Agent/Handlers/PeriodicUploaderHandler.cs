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
            var isDirectory = _fileStreamerWrapper.DirectoryExists(uploadAction.DirName);
            if (isDirectory && actionToReport.TwinReport.PeriodicReported is null)
            {
                actionToReport.TwinReport.PeriodicReported = new Dictionary<string, TwinActionReported>();
            }
            _twinReportHandler.SetReportProperties(actionToReport, StatusType.InProgress);
            await _twinReportHandler.UpdateReportActionAsync(Enumerable.Repeat(actionToReport, 1), cancellationToken);

            string[] files = isDirectory ? _fileStreamerWrapper.GetFiles(uploadAction.DirName, "*", SearchOption.AllDirectories) :
                new string[] { uploadAction.DirName };

            if (files.Count() == 0)
            {
                _logger.Info($"UploadPeriodicAsync: {uploadAction.DirName} is empty");
            }
            foreach (var file in files)
            {
                var key = isDirectory ? _twinReportHandler.GetPeriodicReportedKey(uploadAction, file) : "";
                if (isDirectory && !actionToReport.TwinReport.PeriodicReported.ContainsKey(key))
                {
                    actionToReport.TwinReport.PeriodicReported.Add(key, new TwinActionReported() { Status = StatusType.Pending });
                }                

                var currentCheckSum = await GetFileCheckSumAsync(file);
                if (currentCheckSum != _twinReportHandler.GetActionToReport(actionToReport, file).CheckSum)
                {
                    await _fileUploaderHandler.FileUploadAsync(actionToReport, uploadAction.Method, file, changeSpecId, cancellationToken);
                }
                else
                {
                    _logger.Info($"UploadPeriodicAsync: {file} is up to date");
                }
            }

            if (uploadAction.Interval > 0)
            {
                _twinReportHandler.SetReportProperties(actionToReport, StatusType.Idle);
                Task.Run(async () => IdlePeriodicAsync(actionToReport, changeSpecId, cancellationToken));
            }
            else
            {
                _logger.Info($"Upload periodic of directory {uploadAction.DirName} is success because interval is empty");
                _twinReportHandler.SetReportProperties(actionToReport, StatusType.Success, null, "Interval is empty");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Periodic upload error '{uploadAction.DirName}': {ex.Message}");
            _twinReportHandler.SetReportProperties(actionToReport, StatusType.Failed, ex.Message, ex.GetType().Name);
        }
        finally
        {
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
