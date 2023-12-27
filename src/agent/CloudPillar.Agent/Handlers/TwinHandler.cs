using System.Runtime.InteropServices;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Newtonsoft.Json;
using System.Reflection;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Options;
using Shared.Entities.Utilities;
using System.Text;

namespace CloudPillar.Agent.Handlers;


public class TwinHandler : ITwinHandler
{
    private readonly IDeviceClientWrapper _deviceClient;
    private readonly IFileDownloadHandler _fileDownloadHandler;
    private readonly IFileUploaderHandler _fileUploaderHandler;
    private readonly ITwinReportHandler _twinReportHandler;
    private readonly IStrictModeHandler _strictModeHandler;
    private readonly StrictModeSettings _strictModeSettings;
    private readonly ISignatureHandler _signatureHandler;
    private readonly ILoggerHandler _logger;
    private static Twin? _latestTwin { get; set; }

    public TwinHandler(IDeviceClientWrapper deviceClientWrapper,
                       IFileDownloadHandler fileDownloadHandler,
                       IFileUploaderHandler fileUploaderHandler,
                       ITwinReportHandler twinActionsHandler,
                       ILoggerHandler loggerHandler,
                       IStrictModeHandler strictModeHandler,
                       IOptions<StrictModeSettings> strictModeSettings,
                       ISignatureHandler signatureHandler)
    {
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _fileDownloadHandler = fileDownloadHandler ?? throw new ArgumentNullException(nameof(fileDownloadHandler));
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _twinReportHandler = twinActionsHandler ?? throw new ArgumentNullException(nameof(twinActionsHandler));
        _strictModeHandler = strictModeHandler ?? throw new ArgumentNullException(nameof(strictModeHandler));
        _strictModeSettings = strictModeSettings.Value ?? throw new ArgumentNullException(nameof(strictModeSettings));
        _signatureHandler = signatureHandler ?? throw new ArgumentNullException(nameof(signatureHandler));
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
    }

    public async Task OnDesiredPropertiesUpdateAsync(CancellationToken cancellationToken, bool isInitial = false)
    {
        try
        {
            var twin = await _deviceClient.GetTwinAsync(cancellationToken);
            string reportedJson = twin.Properties.Reported.ToJson();
            var twinReported = JsonConvert.DeserializeObject<TwinReported>(reportedJson)!;
            var twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();

            if (string.IsNullOrWhiteSpace(twinDesired?.ChangeSign))
            {
                _logger.Info($"There is no twin change sign, send sign event..");
                await _signatureHandler.SendSignTwinKeyEventAsync(nameof(twinDesired.ChangeSpec), nameof(twinDesired.ChangeSign), cancellationToken);
            }
            else
            {
                byte[] dataToVerify = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(twinDesired.ChangeSpec));
                var isSignValid = await _signatureHandler.VerifySignatureAsync(dataToVerify, twinDesired.ChangeSign);
                var message = isSignValid ? null : "Twin Change signature is invalid";
                await _deviceClient.UpdateReportedPropertiesAsync(nameof(TwinReported.ChangeSign), message, cancellationToken);
                if (isSignValid)
                {
                    foreach (TwinPatchChangeSpec changeSpec in Enum.GetValues(typeof(TwinPatchChangeSpec)))
                    {
                        await HandleTwinUpdatesAsync(twinDesired, twinReported, changeSpec, isInitial, cancellationToken);
                    }
                }
                else
                {
                    _logger.Error(message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"OnDesiredPropertiesUpdate failed message: {ex.Message}");
        }
    }
    public async Task HandleTwinActionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await OnDesiredPropertiesUpdateAsync(cancellationToken, true);
            DesiredPropertyUpdateCallback callback = async (desiredProperties, userContext) =>
                            {
                                _logger.Info($"Desired properties were updated.");
                                await OnDesiredPropertiesUpdateAsync(cancellationToken);
                            };
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(callback, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"HandleTwinActionsAsync failed message: {ex.Message}");
        }

    }

    private async Task HandleTwinUpdatesAsync(TwinDesired twinDesired,
    TwinReported twinReported, TwinPatchChangeSpec changeSpecKey, bool isInitial, CancellationToken cancellationToken)
    {
        var twinDesiredChangeSpec = twinDesired.GetDesiredChangeSpecByKey(changeSpecKey);
        var twinReportedChangeSpec = twinReported.GetReportedChangeSpecByKey(changeSpecKey);

        var actions = await GetActionsToExecAsync(twinDesiredChangeSpec, twinReportedChangeSpec, changeSpecKey, isInitial, cancellationToken);
        _logger.Info($"HandleTwinUpdatesAsync: {actions?.Count()} actions to execute for {changeSpecKey}");

        if (actions?.Count() > 0)
        {
            await HandleTwinActionsAsync(actions, twinDesiredChangeSpec.Id, cancellationToken);
        }
    }


    public async Task<string> GetTwinJsonAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var twin = await _deviceClient.GetTwinAsync(cancellationToken);
            return twin?.ToJson() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetTwinJsonAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task SaveLastTwinAsync(CancellationToken cancellationToken = default)
    {
        var twin = await _deviceClient.GetTwinAsync(cancellationToken);
        _latestTwin = twin;
    }

    public string GetLatestTwin()
    {
        try
        {
            return _latestTwin?.ToJson() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetLatestTwi failed: {ex.Message}");
            throw;
        }
    }

    private async Task HandleTwinActionsAsync(IEnumerable<ActionToReport> actions, string changeSpecId, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var action in actions)
            {
                var isReplacedSuccess = await SetReplaceFilePathByAction(action, cancellationToken);
                if (!isReplacedSuccess)
                {
                    continue;
                }
                switch (action.TwinAction)
                {
                    case DownloadAction downloadAction:
                        await _fileDownloadHandler.InitFileDownloadAsync(action, cancellationToken);
                        break;

                    case PeriodicUploadAction uploadAction:
                        break;

                    case UploadAction uploadAction:
                        await _fileUploaderHandler.FileUploadAsync(action, uploadAction.Method, uploadAction.FileName, changeSpecId, cancellationToken);
                        break;

                    case ExecuteAction execOnce when _strictModeSettings.StrictMode:
                        _logger.Info("Strict Mode is active, Bash/PowerShell actions are not allowed");
                        await UpdateTwinReportedAsync(action, StatusType.Failed, ResultCode.StrictModeBashPowerShell.ToString(), cancellationToken);
                        break;

                    default:
                        await UpdateTwinReportedAsync(action, StatusType.Failed, ResultCode.NotFound.ToString(), cancellationToken);
                        _logger.Info($"HandleTwinActions, no handler found for index: {action.ReportIndex}");
                        break;
                }

            }
        }
        catch (Exception ex)
        {
            _logger.Error($"HandleTwinActions failed message: {ex.Message}");
        }
    }
    private async Task<bool> SetReplaceFilePathByAction(ActionToReport action, CancellationToken cancellationToken)
    {
        try
        {
            var actionFileName = action.TwinAction switch
            {
                DownloadAction downloadAction => downloadAction.DestinationPath,
                UploadAction uploadAction => uploadAction.FileName,
                PeriodicUploadAction uploadAction => uploadAction.DirName,
                _ => string.Empty
            };
            var filePath = _strictModeHandler.ReplaceRootById(action.TwinAction.Action!.Value, actionFileName) ?? actionFileName;
            switch (action.TwinAction)
            {
                case DownloadAction downloadAction: downloadAction.DestinationPath = filePath; break;
                case UploadAction uploadAction: uploadAction.FileName = filePath; break;
                case PeriodicUploadAction uploadAction: uploadAction.DirName = filePath; break;
            }
            return true;
        }
        catch (Exception ex)
        {
            await UpdateTwinReportedAsync(action, StatusType.Failed, ex.Message, cancellationToken);
            return false;
        }
    }
    private async Task UpdateTwinReportedAsync(ActionToReport action, StatusType statusType, string resultCode, CancellationToken cancellationToken)
    {
        action.TwinReport.Status = statusType;
        action.TwinReport.ResultCode = resultCode;
        await _twinReportHandler.UpdateReportActionAsync(new List<ActionToReport>() { action }, cancellationToken);
    }

    private async Task<IEnumerable<ActionToReport>?> GetActionsToExecAsync(TwinChangeSpec twinDesiredChangeSpec, TwinReportedChangeSpec twinReportedChangeSpec, TwinPatchChangeSpec changeSpecKey, bool isInitial, CancellationToken cancellationToken)
    {
        try
        {
            var isReportedChanged = false;
            var actions = new List<ActionToReport>();
            twinReportedChangeSpec ??= new TwinReportedChangeSpec();
            if (twinReportedChangeSpec.Patch == null || twinReportedChangeSpec.Id != twinDesiredChangeSpec.Id)
            {
                twinReportedChangeSpec.Patch = new TwinReportedPatch();
                twinReportedChangeSpec.Id = twinDesiredChangeSpec.Id;
                isReportedChanged = true;
                _fileDownloadHandler.InitDownloadsList();
            }

            PropertyInfo[] properties = typeof(TwinPatch).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    var desiredValue = (TwinAction[]?)property.GetValue(twinDesiredChangeSpec.Patch);
                    if (desiredValue?.Length > 0)
                    {
                        var reportedProp = typeof(TwinReportedPatch).GetProperty(property.Name);
                        var reportedValue = ((TwinActionReported[])(reportedProp?.GetValue(twinReportedChangeSpec.Patch) ?? new TwinActionReported[0])).ToList();

                        while (reportedValue.Count < desiredValue.Length)
                        {
                            reportedValue.Add(
                                new TwinActionReported() { Status = StatusType.Pending });
                            isReportedChanged = true;
                        }

                        reportedProp?.SetValue(twinReportedChangeSpec.Patch, reportedValue.ToArray());
                        actions.AddRange(desiredValue
                           .Select((item, index) => new ActionToReport(changeSpecKey, twinDesiredChangeSpec.Id)
                           {
                               ReportPartName = property.Name,
                               ReportIndex = index,
                               TwinAction = item,
                               TwinReport = reportedValue[index]
                           })
                        .Where((item, index) => reportedValue[index].Status == StatusType.Pending || reportedValue[index].Status == StatusType.SentForSignature
                            || (isInitial && reportedValue[index].Status != StatusType.Success && reportedValue[index].Status != StatusType.Failed)));


                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"GetActionsToExec failed , desired part: {property.Name} exception: {ex.Message}");
                    continue;
                }
            }
            if (isReportedChanged)
            {
                await _twinReportHandler.UpdateReportedChangeSpecAsync(twinReportedChangeSpec, changeSpecKey, cancellationToken);
            }
            return actions;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetActionsToExec failed: {ex.Message}");
            return null;
        }
    }

}