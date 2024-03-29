﻿using System.Collections.Concurrent;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Services;
using Microsoft.Extensions.Options;
using System.Security.AccessControl;
using System.IO.Compression;

namespace CloudPillar.Agent.Handlers;

public class FileDownloadHandler : IFileDownloadHandler
{
    private readonly IFileStreamerWrapper _fileStreamerWrapper;
    private readonly ID2CMessengerHandler _d2CMessengerHandler;
    private readonly IStrictModeHandler _strictModeHandler;
    private readonly ITwinReportHandler _twinReportHandler;
    private readonly ISignatureHandler _signatureHandler;
    private readonly StrictModeSettings _strictModeSettings;
    private readonly IServerIdentityHandler _serverIdentityHandler;
    private static List<FileDownload> _filesDownloads = new List<FileDownload>();


    private readonly ILoggerHandler _logger;
    private readonly ICheckSumService _checkSumService;
    private readonly DownloadSettings _downloadSettings;

    public FileDownloadHandler(IFileStreamerWrapper fileStreamerWrapper,
                               ID2CMessengerHandler d2CMessengerHandler,
                               IStrictModeHandler strictModeHandler,
                               ITwinReportHandler twinReportHandler,
                               ILoggerHandler loggerHandler,
                               ICheckSumService checkSumService,
                               ISignatureHandler signatureHandler,
                               IServerIdentityHandler serverIdentityHandler,
                               IOptions<StrictModeSettings> strictModeSettings,
                               IOptions<DownloadSettings> options)
    {
        _fileStreamerWrapper = fileStreamerWrapper ?? throw new ArgumentNullException(nameof(fileStreamerWrapper));
        _d2CMessengerHandler = d2CMessengerHandler ?? throw new ArgumentNullException(nameof(d2CMessengerHandler));
        _strictModeHandler = strictModeHandler ?? throw new ArgumentNullException(nameof(strictModeHandler));
        _twinReportHandler = twinReportHandler ?? throw new ArgumentNullException(nameof(twinReportHandler));
        _signatureHandler = signatureHandler ?? throw new ArgumentNullException(nameof(signatureHandler));
        _logger = loggerHandler ?? throw new ArgumentNullException(nameof(loggerHandler));
        _checkSumService = checkSumService ?? throw new ArgumentNullException(nameof(checkSumService));
        _downloadSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _strictModeSettings = strictModeSettings.Value ?? throw new ArgumentNullException(nameof(strictModeSettings));
        _serverIdentityHandler = serverIdentityHandler ?? throw new ArgumentNullException(nameof(serverIdentityHandler));
    }

    public bool AddFileDownload(ActionToReport actionToReport)
    {
        var fileDownload = GetDownloadFile(actionToReport.ReportIndex, ((DownloadAction)actionToReport.TwinAction).Source, actionToReport.ChangeSpecId);
        if (fileDownload == null)
        {
            var destPath = ((DownloadAction)actionToReport.TwinAction).DestinationPath;
            var existFile = _filesDownloads.Any(item =>
                string.IsNullOrWhiteSpace(destPath) ||
                _fileStreamerWrapper.GetFullPath(item.Action.DestinationPath) ==
                _fileStreamerWrapper.GetFullPath(destPath));
            if (existFile)
            {
                _logger.Warn($"File {destPath} is already exist in download list");
                return false;
            }
            fileDownload = new FileDownload { ActionReported = actionToReport };
            _filesDownloads.Add(fileDownload);
        }
        fileDownload.Action.Sign = ((DownloadAction)actionToReport.TwinAction).Sign;
        return true;
    }

    public async Task InitFileDownloadAsync(ActionToReport actionToReport, CancellationToken cancellationToken)
    {
        var file = GetDownloadFile(actionToReport.ReportIndex, ((DownloadAction)actionToReport.TwinAction).Source, actionToReport.ChangeSpecId);
        if (file != null)
        {

            try
            {
                _strictModeHandler.CheckFileAccessPermissions(TwinActionType.SingularDownload, file.Action.DestinationPath);
                if (await ChangeSignExists(file, cancellationToken))
                {
                    ArgumentNullException.ThrowIfNullOrEmpty(file.Action.DestinationPath);
                    var isCreatedDownloadDirectory = InitDownloadPath(file);
                    var destPath = GetDestinationPath(file);
                    var isFileExist = _fileStreamerWrapper.FileExists(destPath);
                    if (file.Report.Status == StatusType.InProgress && !isFileExist) // init inprogress file if it not exist
                    {
                        file.TotalBytesDownloaded = 0;
                        file.Report.Progress = 0;
                        file.Report.Status = StatusType.Pending;
                        file.Report.CompletedRanges = "";
                    }
                    else
                    {
                        var isBlocked = await HandleBlockedStatusAsync(file, isFileExist, destPath, isCreatedDownloadDirectory, cancellationToken);
                        if (!isBlocked)
                        {
                            if (file.Report.Status == StatusType.Unzip)
                            {
                                await HandleCompletedDownloadAsync(file, cancellationToken);
                            }
                            else
                            {
                                if (isFileExist && file.TotalBytesDownloaded == 0)
                                {
                                    file.TotalBytesDownloaded = _fileStreamerWrapper.GetFileLength(destPath);
                                }
                                if (file.Report.Status != StatusType.InProgress)
                                {
                                    await _d2CMessengerHandler.SendFileDownloadEventAsync(cancellationToken, file.ActionReported.ChangeSpecId, file.Action.Source, file.ActionReported.ReportIndex);
                                }
                                else
                                {
                                    await CheckIfNotRecivedDownloadMsgToFile(file, cancellationToken);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleDownloadException(ex, file);
            }
            finally
            {
                if (file.Report.Status != null)
                {
                    await SaveReportAsync(file, cancellationToken);
                }
            }
        }
        else
        {
            _logger.Error($"InitFileDownloadAsync, There is no active download for message {actionToReport.ReportIndex}");
        }
    }

    private async Task<bool> HandleBlockedStatusAsync(FileDownload file, bool isFileExist, string destPath, bool isCreatedDownloadDirectory, CancellationToken cancellationToken)
    {
        if (file.Report.Status == StatusType.Blocked)
        {
            file.Report.Status = StatusType.Pending;
        }
        if ((isFileExist || (!isCreatedDownloadDirectory && file.Action.Unzip))
                     && file.Report.Status is not StatusType.InProgress && file.Report.Status is not StatusType.Unzip)
        {
            SetBlockedStatus(file, DownloadBlocked.FileAlreadyExist, cancellationToken);
        }
        else if (!_fileStreamerWrapper.isSpaceOnDisk(destPath, file.TotalBytes))
        {
            SetBlockedStatus(file, DownloadBlocked.NotEnoughSpace, cancellationToken);
        }
        else if (!HasWritePermissionOnDir(Path.GetDirectoryName(file.Action.DestinationPath)))
        {
            SetBlockedStatus(file, DownloadBlocked.AccessDenied, cancellationToken);
        }
        return file.Report.Status == StatusType.Blocked;
    }

    private async Task<bool> ChangeSignExists(FileDownload file, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(file?.Action?.Sign))
        {
            return true;
        }
        if (!_strictModeSettings.StrictMode)
        {
            _logger.Info("No sign file key is sent, sending for signature");
            await SendForSignatureAsync(file?.ActionReported, cancellationToken);
            return false;
        }
        else
        {
            throw new ArgumentNullException("Sign file key is required");
        }
    }

    private void SetBlockedStatus(FileDownload file, DownloadBlocked resultCode, CancellationToken cancellationToken)
    {
        file.Report.Status = StatusType.Blocked;
        file.Report.ResultCode = resultCode.ToString();
        _logger.Info($"File {file.Action.DestinationPath} sending blocked status, ResultCode: {resultCode}");
        if (!cancellationToken.IsCancellationRequested)
        {
            Task.Run(async () => WaitInBlockedBeforeDownload(file, cancellationToken));
        }
    }

    private async Task WaitInBlockedBeforeDownload(FileDownload file, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(_downloadSettings.BlockedDelayMinutes), cancellationToken);
        var fileDownload = GetDownloadFile(file.ActionReported.ReportIndex, file.Action.Source, file.ActionReported.ChangeSpecId);
        if (!cancellationToken.IsCancellationRequested && fileDownload is not null)
        {
            await InitFileDownloadAsync(file.ActionReported, cancellationToken);
        }
    }

    private async Task CheckIfNotRecivedDownloadMsgToFile(FileDownload file, CancellationToken cancellationToken)
    {
        var downloadedBytes = file.TotalBytesDownloaded;
        var existRanges = file.Report.CompletedRanges;
        await Task.Delay(TimeSpan.FromSeconds(_downloadSettings.CommunicationDelaySeconds), cancellationToken);
        if (!cancellationToken.IsCancellationRequested)
        {
            var isSameDownloadBytes = downloadedBytes == file.TotalBytesDownloaded && existRanges == file.Report.CompletedRanges;
            if (isSameDownloadBytes)
            {
                _logger.Info($"CheckIfNotRecivedDownloadMsgToFile no change in download bytes, file {file.Action.Source}, report index {file.ActionReported.ReportIndex}");
                var ranges = string.Join(",", GetExistRangesList(existRanges));
                await _d2CMessengerHandler.SendFileDownloadEventAsync(cancellationToken, file.ActionReported.ChangeSpecId, file.Action.Source, file.ActionReported.ReportIndex, ranges);
            }
        }
    }

    private async Task SendForSignatureAsync(ActionToReport actionToReport, CancellationToken cancellationToken)
    {
        actionToReport.TwinReport.Status = StatusType.SentForSignature;
        SignFileEvent signFileEvent = new SignFileEvent()
        {
            MessageType = D2CMessageType.SignFileKey,
            ActionIndex = actionToReport.ReportIndex,
            FileName = ((DownloadAction)actionToReport.TwinAction).Source,
            BufferSize = SharedConstants.SIGN_FILE_BUFFER_SIZE,
            PropName = actionToReport.ReportPartName,
            ChangeSpecId = actionToReport.ChangeSpecId,
            ChangeSpecKey = actionToReport.ChangeSpecKey

        };
        await _d2CMessengerHandler.SendSignFileEventAsync(signFileEvent, cancellationToken);
    }

    private void HandleDownloadException(Exception ex, FileDownload file)
    {
        _logger.Error(ex.Message);
        file.Report.Status = StatusType.Failed;
        file.Report.ResultText = ex.Message;
        file.Report.ResultCode = ex.GetType().Name;
        file.Report.Progress = null;
        file.Report.CompletedRanges = null;
        if (file.Action.Unzip)
        {
            _fileStreamerWrapper.DeleteFolder(file.Action.DestinationPath);
        }
        else
        {
            _fileStreamerWrapper.DeleteFile(GetDestinationPath(file));
        }
    }

    private string GetDestinationPath(FileDownload file)
    {
        return file.Action.Unzip ? _fileStreamerWrapper.Combine(file.Action.DestinationPath, file.Action.Source) :
        file.Action.DestinationPath;
    }

    private bool InitDownloadPath(FileDownload file)
    {
        var extention = _fileStreamerWrapper.GetExtension(file.Action.DestinationPath);
        if (file.Action.Unzip)
        {
            if (_fileStreamerWrapper.GetExtension(file.Action.Source)?.Equals(".zip", StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new ArgumentException("No zip file is sent");
            }
        }
        else if (string.IsNullOrEmpty(extention))
        {
            throw new ArgumentException($"Destination path {file.Action.DestinationPath} is not a file.");
        }
        var directory = file.Action.Unzip ? file.Action.DestinationPath : Path.GetDirectoryName(file.Action.DestinationPath);
        if (!string.IsNullOrWhiteSpace(directory) && !_fileStreamerWrapper.DirectoryExists(directory))
        {
            _fileStreamerWrapper.CreateDirectory(directory);
            return true;
        }
        return false;
    }

    public bool HasWritePermissionOnDir(string directoryPath)
    {
        DirectoryInfo directoryInfo = _fileStreamerWrapper.CreateDirectoryInfo(directoryPath);
        DirectorySecurity directorySecurity = _fileStreamerWrapper.GetAccessControl(directoryInfo);
        AuthorizationRuleCollection accessRules = _fileStreamerWrapper.GetAccessRules(directorySecurity);
        var rules = accessRules?.Cast<FileSystemAccessRule>();
        if (rules is not null && rules.Any(x => x.AccessControlType == AccessControlType.Deny))
        {
            return false;
        }
        return true;
    }

    private void HandleFirstMessageAsync(FileDownload file, DownloadBlobChunkMessage message)
    {
        file.TotalBytes = message.FileSize;
        _strictModeHandler.CheckSizeStrictMode(TwinActionType.SingularDownload, file.TotalBytes, file.Action.DestinationPath);
        file.Stopwatch.Start();
    }

    private async Task HandleCompletedDownloadAsync(FileDownload file, CancellationToken cancellationToken)
    {
        file.Stopwatch?.Stop();
        var destPath = GetDestinationPath(file);
        var isVerify = await _signatureHandler.VerifyFileSignatureAsync(destPath, file.Action.Sign);
        if (isVerify)
        {
            file.Report.ResultCode = file.Report.ResultText = null;
            if (file.Action.Unzip)
            {
                file.Report.Status = StatusType.Unzip;
                await _twinReportHandler.UpdateReportActionAsync(Enumerable.Repeat(file.ActionReported, 1), cancellationToken);
                UnzipFileAsync(destPath, file.Action.DestinationPath);
                _fileStreamerWrapper.DeleteFile(destPath);
                _logger.Info($"Download complete, file {file.Action.Source}, report index {file.ActionReported.ReportIndex}");
            }
            file.Report.Status = StatusType.Success;
            file.Report.Progress = 100;
        }
        else
        {
            var message = $"File {file.Action.DestinationPath} signature is not valid, the file will be deleted.";
            _logger.Error(message);
            throw new Exception(message);
        }
    }

    private async Task HandleEndRangeDownloadAsync(string filePath, DownloadBlobChunkMessage message, FileDownload file, CancellationToken cancellationToken)
    {
        var isRangeValid = await VerifyRangeCheckSumAsync(filePath, message.RangeStartPosition.GetValueOrDefault(), message.RangeEndPosition.GetValueOrDefault(), message.RangeCheckSum);
        if (!isRangeValid)
        {
            await _d2CMessengerHandler.SendFileDownloadEventAsync(cancellationToken, message.ChangeSpecId, message.FileName, file.ActionReported.ReportIndex, message.RangeIndex.ToString(), message.RangeStartPosition, message.RangeEndPosition);
            file.TotalBytesDownloaded -= (long)(message.RangeEndPosition - message.RangeStartPosition).GetValueOrDefault();
            if (file.TotalBytesDownloaded < 0) { file.TotalBytesDownloaded = 0; }
        }
        else
        {
            file.Report.CompletedRanges = AddRange(file.Report.CompletedRanges, message.RangeIndex);
        }
    }
    private FileDownload? GetDownloadFile(int actionIndex, string fileName, string changeSpecId)
    {
        var file = _filesDownloads.FirstOrDefault(item => item.ActionReported.ReportIndex == actionIndex &&
                                    item.Action.Source == fileName &&
                                    (string.IsNullOrWhiteSpace(changeSpecId) || item.ActionReported.ChangeSpecId == changeSpecId));
        return file;
    }

    public async Task HandleDownloadMessageAsync(DownloadBlobChunkMessage message, CancellationToken cancellationToken)
    {
        var file = GetDownloadFile(message.ActionIndex, message.FileName, message.ChangeSpecId);
        if (file == null)
        {
            _logger.Error($"There is no active download for message {message.GetMessageId()}");
            return;
        }
        var filePath = GetDestinationPath(file);

        try
        {
            var fileLength = Math.Max(file.TotalBytes - _fileStreamerWrapper.GetFileLength(filePath), message.Offset + message.Data?.Length ?? 0);
            if (!_fileStreamerWrapper.isSpaceOnDisk(filePath, fileLength))
            {
                SetBlockedStatus(file, DownloadBlocked.NotEnoughSpace, cancellationToken);
                _fileStreamerWrapper.DeleteFile(GetDestinationPath(file));
            }
            if (file.Report.Status == StatusType.Blocked)
            {
                _logger.Info($"File {file.Action.DestinationPath} is blocked, message {message.GetMessageId()}");
                return;
            }
            if (!string.IsNullOrWhiteSpace(message.Error))
            {
                throw new Exception($"Backend error: {message.Error}");
            }
            if (!file.Stopwatch.IsRunning)
            {
                HandleFirstMessageAsync(file, message);
            }

            await _fileStreamerWrapper.WriteChunkToFileAsync(filePath, message.Offset, message.Data);

            var downloadedFileBytes = _fileStreamerWrapper.GetFileLength(filePath);
            _strictModeHandler.CheckSizeStrictMode(TwinActionType.SingularDownload, downloadedFileBytes, file.Action.DestinationPath);

            if (message.RangeCheckSum != null)
            {
                await HandleEndRangeDownloadAsync(filePath, message, file, cancellationToken);
            }
            if (file.Report.CompletedRanges == GetCompletedRangesString(message.RangesCount))
            {
                await HandleCompletedDownloadAsync(file, cancellationToken);
            }
            else
            {
                file.Report.Progress = CalculateBytesDownloadedPercent(file, message.Data.Length, message.Offset);
                file.Report.Status = StatusType.InProgress;
                file.Report.ResultCode = file.Report.ResultText = null;
                Task.Run(async () => CheckIfNotRecivedDownloadMsgToFile(file, cancellationToken));
            }
        }
        catch (Exception ex)
        {
            HandleDownloadException(ex, file);
        }
        finally
        {
            await SaveReportAsync(file, cancellationToken);
        }
    }

    private string GetCompletedRangesString(int? rangesCount)
    {
        return rangesCount switch
        {
            1 => "0",
            2 => "0,1",
            _ => $"0-{rangesCount - 1}"
        };
    }


    private async Task SaveReportAsync(FileDownload file, CancellationToken cancellationToken)
    {
        try
        {
            await _twinReportHandler.UpdateReportActionAsync(Enumerable.Repeat(file.ActionReported, 1), cancellationToken);
            if (file.Report.Status == StatusType.Failed || file.Report.Status == StatusType.Success)
            {
                RemoveFileFromList(file.ActionReported.ReportIndex, file.Action.Source, file.ActionReported.ChangeSpecId);
                await UpdateKnownIdentities(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"SaveReportAsync failed message: {ex.Message}");
        }

    }

    private float CalculateBytesDownloadedPercent(FileDownload file, long bytesLength, long offset)
    {
        const double KB = 1024.0;
        file.TotalBytesDownloaded += bytesLength;
        double progressPercent = Math.Round(file.TotalBytesDownloaded / (double)file.TotalBytes * 100, 2);
        double throughput = file.TotalBytesDownloaded / file.Stopwatch.Elapsed.TotalSeconds / KB;
        progressPercent = Math.Min(progressPercent, 99); // for cases that chunk in range send twice
        _logger.Info($"%{progressPercent:00} @pos: {offset:00000000000} Throughput: {throughput:0.00} KiB/s");
        return (float)progressPercent;
    }

    private async Task<bool> VerifyRangeCheckSumAsync(string filePath, long startPosition, long endPosition, string checkSum)
    {
        long lengthToRead = endPosition - startPosition;
        byte[] data = _fileStreamerWrapper.ReadStream(filePath, startPosition, lengthToRead);

        var streamCheckSum = await _checkSumService.CalculateCheckSumAsync(data);
        return checkSum == streamCheckSum;

    }

    /// <summary>
    /// Modifies a string of numerical ranges by adding a specified integer to the existing ranges.
    /// </summary>
    /// <param name="rangesString">String of comma-separated numerical ranges.</param>
    /// <param name="rangeIndex">Integer to be added to the ranges.</param>
    /// <returns>Modified and sorted string of numerical ranges.</returns>
    /// 
    /// <example>
    /// Example usage:
    /// <code>
    /// string modifiedRanges = AddRange("1-5,7,10-12", 9);//"1-5,7,9-12";
    /// </code>
    /// </example>

    private string AddRange(string rangesString, int rangeIndex)
    {
        var ranges = GetExistRangesList(rangesString);

        ranges.Add(rangeIndex);
        ranges.Sort();

        var newRangeString = string.Join(",",
            ranges.Distinct()
                  .Select((value, index) => (value, index))
                  .GroupBy(pair => pair.value - pair.index)
                  .Select(group => group.Select((pair, i) => pair.value))
                  .Select(range => range.Count() == 1 ? $"{range.First()}" :
                                    range.Count() == 2 ? $"{range.First()},{range.Last()}" :
                                    $"{range.First()}-{range.Last()}")
                  .ToList());
        return newRangeString;
    }

    private List<int> GetExistRangesList(string rangesString)
    {
        var ranges = new List<int>();
        if (!string.IsNullOrWhiteSpace(rangesString))
        {
            ranges = rangesString.Split(',')
                .SelectMany(part => part.Contains('-')
                    ? Enumerable.Range(
                        int.Parse(part.Split('-')[0]),
                        int.Parse(part.Split('-')[1]) - int.Parse(part.Split('-')[0]) + 1)
                    : new[] { int.Parse(part) })
                .ToList();
        }
        return ranges;
    }

    private void RemoveFileFromList(int actionIndex, string fileName, string changeSpecId)
    {
        _filesDownloads.RemoveAll(item => (item.Action.Source == fileName || item.ActionReported.ReportIndex == actionIndex)
        && item.ActionReported.ChangeSpecId == changeSpecId);
    }

    public void InitDownloadsList(List<ActionToReport> actions = null)
    {
        _filesDownloads.RemoveAll(item => GetFileFromAction(actions, item) == null);
    }

    private ActionToReport? GetFileFromAction(List<ActionToReport> actions, FileDownload fileDownload)
    {
        var file = actions?.FirstOrDefault(item => item.ReportIndex == fileDownload.ActionReported.ReportIndex &&
                                    ((DownloadAction)item.TwinAction).Source == fileDownload.Action.Source &&
                                    (string.IsNullOrWhiteSpace(fileDownload.ActionReported.ChangeSpecId) || item.ChangeSpecId == fileDownload.ActionReported.ChangeSpecId));
        return file;
    }

    private async Task UpdateKnownIdentities(CancellationToken cancellationToken)
    {
        if (_filesDownloads.Count == 0 && !cancellationToken.IsCancellationRequested)
        {
            await _serverIdentityHandler.UpdateKnownIdentitiesFromCertificatesAsync(cancellationToken);
        }
    }


    public void UnzipFileAsync(string zipPath, string destinationPath)
    {
        using (ZipArchive archive = _fileStreamerWrapper.OpenZipFile(zipPath))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string completeFileName = _fileStreamerWrapper.Combine(destinationPath, entry.FullName);

                _fileStreamerWrapper.CreateDirectory(_fileStreamerWrapper.GetDirectoryName(completeFileName)!);
                if (!entry.FullName.EndsWith("/"))
                {
                    entry.ExtractToFile(completeFileName, overwrite: true);
                    _fileStreamerWrapper.SetLastWriteTimeUtc(completeFileName, entry.LastWriteTime.UtcDateTime);
                }
            }
        }
    }
}