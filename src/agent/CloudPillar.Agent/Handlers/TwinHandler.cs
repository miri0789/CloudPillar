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
    private readonly IPeriodicUploaderHandler _periodicUploaderHandler;
    private static Twin? _latestTwin { get; set; }
    private static CancellationTokenSource? _twinCancellationTokenSource;

    public TwinHandler(IDeviceClientWrapper deviceClientWrapper,
                       IFileDownloadHandler fileDownloadHandler,
                       IFileUploaderHandler fileUploaderHandler,
                       ITwinReportHandler twinActionsHandler,
                       ILoggerHandler loggerHandler,
                       IStrictModeHandler strictModeHandler,
                       IOptions<StrictModeSettings> strictModeSettings,
                       ISignatureHandler signatureHandler,
                       IPeriodicUploaderHandler periodicUploaderHandler)
    {
        _deviceClient = deviceClientWrapper ?? throw new ArgumentNullException(nameof(deviceClientWrapper));
        _fileDownloadHandler = fileDownloadHandler ?? throw new ArgumentNullException(nameof(fileDownloadHandler));
        _fileUploaderHandler = fileUploaderHandler ?? throw new ArgumentNullException(nameof(fileUploaderHandler));
        _twinReportHandler = twinActionsHandler ?? throw new ArgumentNullException(nameof(twinActionsHandler));
        _strictModeHandler = strictModeHandler ?? throw new ArgumentNullException(nameof(strictModeHandler));
        _strictModeSettings = strictModeSettings.Value ?? throw new ArgumentNullException(nameof(strictModeSettings));
        _signatureHandler = signatureHandler ?? throw new ArgumentNullException(nameof(signatureHandler));
        _periodicUploaderHandler = periodicUploaderHandler ?? throw new ArgumentNullException(nameof(periodicUploaderHandler));
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
    }

    public void CancelCancellationToken()
    {
        _twinCancellationTokenSource?.Cancel();
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

    private async Task<TwinReportedChangeSpec> ResetReportedWhenDesiredChange(TwinChangeSpec twinDesiredChangeSpec, TwinReportedChangeSpec twinReportedChangeSpec, string changeSpecKey, bool isInitial, CancellationToken cancellationToken)
    {
        if (twinDesiredChangeSpec?.Id != twinReportedChangeSpec?.Id || isInitial)
        {
            CancelCancellationToken();
            _twinCancellationTokenSource = new CancellationTokenSource();
        }
        var isReportedExist = twinDesiredChangeSpec is null && twinReportedChangeSpec is not null;
        if (isReportedExist ||
        twinDesiredChangeSpec?.Patch?.Count > 0 && twinReportedChangeSpec?.Patch is null ||
        twinReportedChangeSpec?.Id != twinDesiredChangeSpec?.Id)
        {
            if (isReportedExist)
            {
                twinReportedChangeSpec = null;
            }
            else
            {
                twinReportedChangeSpec = new TwinReportedChangeSpec()
                {
                    Patch = twinDesiredChangeSpec.Patch?.ToDictionary(entry => entry.Key, entry => new TwinActionReported[0]),
                    Id = twinDesiredChangeSpec.Id
                };
            }
            await _twinReportHandler.UpdateReportedChangeSpecAsync(twinReportedChangeSpec, changeSpecKey, cancellationToken);
        }
        return twinReportedChangeSpec;
    }

    public async Task OnDesiredPropertiesUpdateAsync(CancellationToken cancellationToken, bool isInitial = false)
    {
        try
        {
            var twin = await _twinReportHandler.SetTwinReported(cancellationToken);
            var twinReported = twin.Properties.Reported.ToJson().ConvertToTwinReported();

            var twinDesired = twin.Properties.Desired.ToJson().ConvertToTwinDesired();

            ResetNotActualDownloads(twinDesired, twinReported);
            foreach (string changeSpecKey in twinDesired.ChangeSpec.Keys)
            {
                var twinDesiredChangeSpec = twinDesired?.GetDesiredChangeSpecByKey(changeSpecKey);
                var twinReportedChangeSpec = twinReported?.GetReportedChangeSpecByKey(changeSpecKey);
                twinReportedChangeSpec = await ResetReportedWhenDesiredChange(twinDesiredChangeSpec, twinReportedChangeSpec, changeSpecKey, isInitial, cancellationToken);
                twinReported.SetReportedChangeSpecByKey(twinReportedChangeSpec, changeSpecKey);

                if (await ChangeSpecIdEmpty(twinDesired, changeSpecKey, cancellationToken))
                {
                    _logger.Info($"There is no twin change spec id for {changeSpecKey}");
                    continue;
                }
                var changeSignKey = changeSpecKey.GetSignKeyByChangeSpec();

                if (await ChangeSignExists(twinDesired, changeSignKey, cancellationToken))
                {
                    byte[] dataToVerify = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(twinDesired.GetDesiredChangeSpecByKey(changeSpecKey)));
                    var isSignValid = await _signatureHandler.VerifySignatureAsync(dataToVerify, twinDesired?.GetDesiredChangeSignByKey(changeSignKey)!);
                    var message = isSignValid ? null : $"Twin Change signature for {changeSignKey} is invalid";
                    await _deviceClient.UpdateReportedPropertiesAsync(changeSignKey, message, cancellationToken);
                    if (isSignValid)
                    {
                        await HandleTwinUpdatesAsync(twinDesired, twinReported, changeSpecKey, isInitial, cancellationToken);

                    }
                    else
                    {
                        _logger.Error(message);
                    }
                }
            }

        }

        catch (Exception ex)
        {
            _logger.Error($"OnDesiredPropertiesUpdate failed message: {ex.Message}");
        }
    }


    private void ResetNotActualDownloads(TwinDesired twinDesired, TwinReported twinReported)
    {
        var actions = twinDesired?.ChangeSpec?
            .SelectMany(desiredChangeSpec =>
                GetActiveDownloads(desiredChangeSpec.Value, twinReported?.GetReportedChangeSpecByKey(desiredChangeSpec.Key),
                    desiredChangeSpec.Key)).ToList();
        if (actions.Count() > 0)
        {
            _fileDownloadHandler.InitDownloadsList(actions);
        }
    }

    private async Task HandleTwinUpdatesAsync(TwinDesired twinDesired,
    TwinReported twinReported, string changeSpecKey, bool isInitial, CancellationToken cancellationToken)
    {
        var twinDesiredChangeSpec = twinDesired.GetDesiredChangeSpecByKey(changeSpecKey.ToString());
        var twinReportedChangeSpec = twinReported.GetReportedChangeSpecByKey(changeSpecKey.ToString());

        var actions = await GetActionsToExecAsync(twinDesiredChangeSpec!, twinReportedChangeSpec!, changeSpecKey, isInitial, cancellationToken);

        if (actions is not null)
        {

            foreach (var action in actions)
            {
                await SetReplaceFilePathByAction(action, cancellationToken);
                if (action.TwinAction is DownloadAction downloadAction)
                {
                    var isDuplicate = !_fileDownloadHandler.AddFileDownload(action);
                    if (isDuplicate)
                    {
                        action.TwinReport.Status = StatusType.Duplicate;
                    }
                }
            }

            var duplicateActions = actions.Where(action => action.TwinReport.Status == StatusType.Duplicate);
            foreach (var action in duplicateActions)
            {
                await UpdateTwinReportedAsync(action, StatusType.Duplicate, null, cancellationToken);
            }

            _logger.Info($"HandleTwinUpdatesAsync: {actions?.Count()} actions to execute for {changeSpecKey}");

            if (actions?.Count() > 0)
            {
                Task.Run(async () => HandleTwinActionsAsync(actions, twinDesiredChangeSpec.Id, cancellationToken));
            }
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

    private bool IsActiveAction(TwinActionReported action)
    {
        return action.Status != StatusType.Failed && action.Status != StatusType.Success && action.Status != StatusType.Duplicate;
    }

    private async Task HandleTwinActionsAsync(IEnumerable<ActionToReport> actions, string changeSpecId, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var action in actions.Where(action => IsActiveAction(action.TwinReport)))
            {
                switch (action.TwinAction)
                {
                    case DownloadAction downloadAction:
                        await _fileDownloadHandler.InitFileDownloadAsync(action, _twinCancellationTokenSource!.Token);
                        break;

                    case PeriodicUploadAction uploadAction:
                        await _periodicUploaderHandler.UploadAsync(action, changeSpecId, _twinCancellationTokenSource!.Token);
                        break;

                    case UploadAction uploadAction:
                        await _fileUploaderHandler.FileUploadAsync(action, uploadAction.Method, uploadAction.FileName, changeSpecId, _twinCancellationTokenSource!.Token);
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
                PeriodicUploadAction uploadAction => uploadAction.DirName ?? uploadAction.FileName,
                _ => string.Empty
            };
            var filePath = _strictModeHandler.ReplaceRootById(action.TwinAction.Action!.Value, actionFileName) ?? actionFileName;
            if (filePath is not null && filePath.StartsWith("."))
            {
                filePath = filePath[2..].Replace("/", "\\");
                filePath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            }
            switch (action.TwinAction)
            {
                case DownloadAction downloadAction: downloadAction.DestinationPath = filePath; break;
                case UploadAction uploadAction: uploadAction.FileName = filePath; break;
                case PeriodicUploadAction uploadAction:
                    if (!string.IsNullOrWhiteSpace(uploadAction.DirName))
                    {
                        uploadAction.DirName = filePath;
                    }
                    else
                    {
                        uploadAction.FileName = filePath;
                    }
                    break;
            }
            return true;
        }
        catch (Exception ex)
        {
            await UpdateTwinReportedAsync(action, StatusType.Failed, ex.Message, cancellationToken);
            return false;
        }
    }
    private async Task UpdateTwinReportedAsync(ActionToReport action, StatusType statusType, string? resultCode, CancellationToken cancellationToken)
    {
        action.TwinReport.Status = statusType;
        action.TwinReport.ResultCode = resultCode;
        await _twinReportHandler.UpdateReportActionAsync(new List<ActionToReport>() { action }, cancellationToken);
    }

    private async Task<IEnumerable<ActionToReport>?> GetActionsToExecAsync(TwinChangeSpec twinDesiredChangeSpec, TwinReportedChangeSpec twinReportedChangeSpec, string changeSpecKey, bool isInitial, CancellationToken cancellationToken)
    {
        try
        {
            var isReportedChanged = false;
            var actions = new List<ActionToReport>();
            twinReportedChangeSpec ??= new TwinReportedChangeSpec();

            foreach (var desired in twinDesiredChangeSpec.Patch!)
            {
                try
                {
                    if (!twinReportedChangeSpec.Patch.ContainsKey(desired.Key))
                    {
                        twinReportedChangeSpec.Patch.Add(desired.Key, new TwinActionReported[0]);
                        isReportedChanged = true;
                    }
                    var itemReported = twinReportedChangeSpec.Patch![desired.Key].ToList();

                    while (itemReported.Count() < desired.Value.Count())
                    {
                        itemReported.Add(new TwinActionReported() { Status = StatusType.Pending });
                        isReportedChanged = true;
                    }
                    twinReportedChangeSpec.Patch[desired.Key] = itemReported.ToArray();

                    actions.AddRange(desired.Value
                       .Select((item, index) => new ActionToReport(changeSpecKey, twinDesiredChangeSpec.Id!)
                       {
                           ReportPartName = desired.Key,
                           ReportIndex = index,
                           TwinAction = item,
                           TwinReport = itemReported[index]
                       })
                    .Where((item, index) => itemReported[index].Status == StatusType.Pending || itemReported[index].Status == StatusType.SentForSignature
                        || (isInitial && IsActiveAction(itemReported[index]))));


                }
                catch (Exception ex)
                {
                    _logger.Error($"GetActionsToExec failed , desired part: {desired.Key} exception: {ex.Message}");
                    continue;
                }
            }
            if (isReportedChanged)
            {
                await _twinReportHandler.UpdateReportedChangeSpecAsync(twinReportedChangeSpec, changeSpecKey.ToString(), cancellationToken);
            }
            return actions;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetActionsToExec failed: {ex.Message}");
            return null;
        }
    }

    private IEnumerable<ActionToReport> GetActiveDownloads(TwinChangeSpec twinDesiredChangeSpec,
    TwinReportedChangeSpec twinReportedChangeSpec, string changeSpecKey)
    {
        try
        {
            var actions = new List<ActionToReport>();
            twinReportedChangeSpec ??= new TwinReportedChangeSpec();

            foreach (var desired in twinDesiredChangeSpec.Patch!)
            {
                try
                {
                    actions.AddRange(desired.Value
                       .Select((item, index) => new ActionToReport(changeSpecKey, twinDesiredChangeSpec.Id!)
                       {
                           ReportPartName = desired.Key,
                           ReportIndex = index,
                           TwinAction = item
                       })
                    .Where((item, index) => twinReportedChangeSpec.Patch is null || (twinReportedChangeSpec.Patch?[desired.Key].Length <= index)
                    || IsActiveAction(twinReportedChangeSpec.Patch![desired.Key][index])));

                }
                catch (Exception ex)
                {
                    _logger.Error($"GetActionsToExec failed , desired part: {desired.Key} exception: {ex.Message}");
                    continue;
                }
            }
            return actions;
        }
        catch (Exception ex)
        {
            _logger.Error($"GetActionsToExec failed: {ex.Message}");
            return null;
        }
    }

    private async Task<bool> ChangeSpecIdEmpty(TwinDesired twinDesired, string? changeSpecKey, CancellationToken cancellationToken)
    {
        var changeSpecIdKey = changeSpecKey.GetChangeSpecIdKeyByChangeSpecKey();
        var changeSpec = twinDesired?.GetDesiredChangeSpecByKey(changeSpecKey);
        var emptyChangeSpecId = string.IsNullOrWhiteSpace(changeSpec.Id);
        var message = emptyChangeSpecId ? $"There is no ID for {changeSpecKey}.." : null;
        await _deviceClient.UpdateReportedPropertiesAsync(changeSpecIdKey, message, cancellationToken);
        return emptyChangeSpecId;
    }

    private async Task<bool> ChangeSignExists(TwinDesired twinDesired, string changeSignKey, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(twinDesired?.GetDesiredChangeSignByKey(changeSignKey)?.ToString()))
        {
            return true;
        }
        if (!_strictModeSettings.StrictMode)
        {
            _logger.Info($"There is no twin change sign, send sign event..");
            await _signatureHandler.SendSignTwinKeyEventAsync(changeSignKey, cancellationToken);
            return false;
        }
        else
        {
            _logger.Info($"There is no twin change sign, strict mode is active");
            await _deviceClient.UpdateReportedPropertiesAsync(changeSignKey, "Change sign is required", cancellationToken);
            return false;
        }
    }
}