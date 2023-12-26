using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Twin;

namespace CloudPillar.Agent.Handlers;

public class PeriodicUploaderHandler : IPeriodicUploaderHandler
{
    private readonly ILoggerHandler _logger;
    private readonly IFileStreamerWrapper _fileStreamerWrapper;

    public PeriodicUploaderHandler(ILoggerHandler logger,
                                   IFileStreamerWrapper fileStreamerWrapper)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
    }


    public async Task UploadAsync(ActionToReport actionToReport, CancellationToken cancellationToken)
    {
        var uploadAction = (PeriodicUploadAction)actionToReport.TwinAction;
        var isDirectory =string.IsNullOrWhiteSpace(_fileStreamerWrapper.GetExtension(uploadAction.DirName)) ;
        _logger.Info("UploadAsync: start");
        try
        {

//  string[] files = Directory ontent of file: {filePath}");
            


            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            _logger.Info("UploadAsync: end");
        }
        catch (Exception ex)
        {
            _logger.Error($"UploadAsync: {ex.Message}");
        }
    }
}
