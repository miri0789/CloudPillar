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

    public PeriodicUploaderHandler(ILoggerHandler logger,
                                   IFileStreamerWrapper fileStreamerWrapper,
                                   IFileUploaderHandler fileUploaderHandler,
                                   ICheckSumService checkSumService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
    }


    public async Task UploadAsync(ActionToReport actionToReport, string changeSpecId, CancellationToken cancellationToken)
    {
        var uploadAction = (PeriodicUploadAction)actionToReport.TwinAction;
        var isDirectory = _fileStreamerWrapper.DirectoryExists(uploadAction.DirName);
        _logger.Info("UploadAsync: start");
        if (actionToReport.TwinReport.PeriodicReported == null)
        {
            actionToReport.TwinReport.PeriodicReported = new Dictionary<string, TwinActionReported>();
        }
        try
        {// IDLE INPROGRESS   checked if interval <0 // if folder is empty : report it
         //timer




            string[] files = isDirectory ? _fileStreamerWrapper.GetFiles(uploadAction.DirName, "*", SearchOption.AllDirectories) :
                new string[] { uploadAction.DirName };
            foreach (var file in files)
            {
                var key = file.Substring(uploadAction.DirName.Length)
                .Replace(FileConstants.SEPARATOR, FileConstants.DOUBLE_SEPARATOR)
                .Replace(FileConstants.DOT,"_");
                if (isDirectory && !actionToReport.TwinReport.PeriodicReported.ContainsKey(key))
                {
                    actionToReport.TwinReport.PeriodicReported.Add(key
                    , new TwinActionReported() { Status = StatusType.Pending });
                }

                var currentCheckSum = string.Empty;
                using (Stream readStream = _fileStreamerWrapper.CreateStream(file, FileMode.Open, FileAccess.Read))
                {
                    currentCheckSum = await _checkSumService.CalculateCheckSumAsync(readStream);
                }
                if (currentCheckSum != actionToReport.TwinReport.PeriodicReported[key].CheckSum)
                {
                    await _fileUploaderHandler.FileUploadAsync(actionToReport, uploadAction.Method, file, changeSpecId, cancellationToken);
                }
                else
                {
                    _logger.Info($"UploadPeriodicAsync: {file} is up to date");
                }

            }




            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            _logger.Info("UploadAsync: end");
        }
        catch (Exception ex)
        {
            _logger.Error($"UploadAsync: {ex.Message}");
        }
    }
}
