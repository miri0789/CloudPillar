using Moq;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using Shared.Entities.Services;
using Shared.Entities.Messages;
using Shared.Enums;

namespace CloudPillar.Agent.Tests
{
    [TestFixture]
    public class FileDownloadHandlerTestFixture
    {
        private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
        private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
        private Mock<IStrictModeHandler> _strictModeHandlerMock;
        private Mock<ILoggerHandler> _loggerMock;
        private Mock<ITwinReportHandler> _twinReportHandlerMock;
        private Mock<ISignatureHandler> _signatureHandlerMock;

        private Mock<ICheckSumService> _checkSumServiceMock;
        private IFileDownloadHandler _target;
        private StrictModeSettings mockStrictModeSettingsValue = new StrictModeSettings();
        private Mock<IOptions<StrictModeSettings>> _mockStrictModeSettings;
        private Mock<IOptions<DownloadSettings>> mockDownloadSettings;
        private Mock<IServerIdentityHandler> _serverIdentityHandlerMock;

        private int actionIndex = 0;

        [SetUp]
        public void Setup()
        {

            mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock();
            _mockStrictModeSettings = new Mock<IOptions<StrictModeSettings>>();
            _mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);

            mockDownloadSettings = new Mock<IOptions<DownloadSettings>>();
            mockDownloadSettings.Setup(x => x.Value).Returns(new DownloadSettings());

            _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
            _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
            _strictModeHandlerMock = new Mock<IStrictModeHandler>();
            _signatureHandlerMock = new Mock<ISignatureHandler>();
            _twinReportHandlerMock = new Mock<ITwinReportHandler>();
            _checkSumServiceMock = new Mock<ICheckSumService>();
            _loggerMock = new Mock<ILoggerHandler>();
            _serverIdentityHandlerMock = new Mock<IServerIdentityHandler>();

            _fileStreamerWrapperMock.Setup(item => item.isSpaceOnDisk(It.IsAny<string>(), It.IsAny<long>())).Returns(true);
            _fileStreamerWrapperMock.Setup(f => f.GetExtension(It.IsAny<string>())).Returns(".zip");
            _fileStreamerWrapperMock.Setup(x => x.ReadStream(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>()))
                                    .Returns(new byte[0]);

            CreateTarget();
        }

        private void CreateTarget()
        {
            _target = new FileDownloadHandler(_fileStreamerWrapperMock.Object,
              _d2CMessengerHandlerMock.Object,
              _strictModeHandlerMock.Object,
              _twinReportHandlerMock.Object,
              _loggerMock.Object,
              _checkSumServiceMock.Object,
              _signatureHandlerMock.Object,
              _serverIdentityHandlerMock.Object,
              _mockStrictModeSettings.Object,
               mockDownloadSettings.Object);
        }
        private FileDownload initAction()
        {
            var action = new FileDownload()
            {
                ActionReported = new ActionToReport()
                {
                    ReportIndex = actionIndex++,
                    TwinReport = new TwinActionReported(),
                    TwinAction = new DownloadAction()
                    {
                        Source = "file.txt",
                        DestinationPath = "C:\\Downloads",
                        Sign = "aaaaaa"
                    }
                }
            };
            return action;
        }


        [Test]
        public async Task InitFileDownloadAsync_NewDownload_SendFileDownloadEvent()
        {
            var action = initAction();
            await InitFileDownloadAsync(action);

            _d2CMessengerHandlerMock.Verify(mf => mf.SendFileDownloadEventAsync(It.IsAny<CancellationToken>(), It.IsAny<string>(), action.Action.Source, action.ActionReported.ReportIndex, It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<long?>()), Times.Once);

        }

        [Test]
        public async Task InitFileDownloadAsync_NewDownloadWithoutSign_SendForSignature()
        {
            mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock(false);
            _mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);
            CreateTarget();

            var action = initAction();
            action.Action.Sign = null;
            await InitFileDownloadAsync(action);

            _d2CMessengerHandlerMock.Verify(mf => mf.SendSignFileEventAsync(It.IsAny<SignFileEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task InitFileDownloadAsync_NewDownloadWithoutSignStrictModeTrue_UpdateReportedFailed()
        {
            mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock(true);
            _mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);
            CreateTarget();

            var action = initAction();
            action.Action.Sign = null;
            await InitFileDownloadAsync(action);

            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item => item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task InitFileDownloadAsync_NewDownloadWithoutSign_NotSendDownload()
        {
            var action = initAction();
            action.Action.Sign = null;
            await InitFileDownloadAsync(action);

            _d2CMessengerHandlerMock.Verify(mf => mf.SendFileDownloadEventAsync(It.IsAny<CancellationToken>(), It.IsAny<string>(), action.Action.Source, action.ActionReported.ReportIndex, "0", It.IsAny<long?>(), It.IsAny<long?>()), Times.Never);
        }

        [Test]
        public async Task InitFileDownloadAsync_ActiveDownload_SendFileDownloadEventWithRange()
        {
            var action = initAction();
            action.Report.CompletedRanges = "0-5,8,10";
            await InitFileDownloadAsync(action);

            _d2CMessengerHandlerMock.Verify(mf => mf.SendFileDownloadEventAsync(It.IsAny<CancellationToken>(), It.IsAny<string>(), action.Action.Source, action.ActionReported.ReportIndex, "6", It.IsAny<long?>(), It.IsAny<long?>()), Times.Never);

        }

        [Test]
        public async Task InitFileDownloadAsync_SendFirmwareEventFailure_UpdateReportedToFaild()
        {
            var action = initAction();
            _d2CMessengerHandlerMock.Setup(dc =>
                    dc.SendFileDownloadEventAsync(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<long?>()))
                    .ThrowsAsync(new Exception());
            await InitFileDownloadAsync(action);

            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item => item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task InitFileDownloadAsync_InvalidFileAccessPermissions_UpdateReportedToFaild()
        {
            _strictModeHandlerMock.Setup(x => x.CheckFileAccessPermissions(TwinActionType.SingularDownload, It.IsAny<string>())).Throws(new Exception());
            var action = initAction();
            await InitFileDownloadAsync(action);

            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item => item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_PartiallyData_ReportInprogressWithProgress()
        {
            var action = initAction();
            await InitFileDownloadAsync(action);

            var message = new DownloadBlobChunkMessage
            {
                ActionIndex = action.ActionReported.ReportIndex,
                FileName = action.Action.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 4096
            };

            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);


            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.InProgress && rep.TwinReport.Progress == 25))
            , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_Failure_DeleteFile()
        {
            var action = initAction();
            await InitFileDownloadAsync(action);

            var message = new DownloadBlobChunkMessage
            {
                ActionIndex = action.ActionReported.ReportIndex,
                FileName = action.Action.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 4096
            };
            _fileStreamerWrapperMock.Setup(f => f.WriteChunkToFileAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>())).ThrowsAsync(new Exception());

            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);


            _fileStreamerWrapperMock.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_SignVerifyFailed_DeleteFile()
        {
            await SetUpHandleDownloadMessageAsync_SignVerifyFailed();
            _fileStreamerWrapperMock.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_SignVerifyFailed_UpdateReport()
        {
            await SetUpHandleDownloadMessageAsync_SignVerifyFailed();
            _twinReportHandlerMock.Verify(
                          x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                          item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
                      , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_InProgressDeletedFile_InitProgress()
        {
            var action = initAction();
            await InitFileDownloadAsync(action);
            action.Report.Progress = 50;
            action.Report.Status = StatusType.InProgress;
            _fileStreamerWrapperMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
            await InitFileDownloadAsync(action);
            _twinReportHandlerMock.Verify(
                           x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                           item.Any(rep => rep.TwinReport.Status == StatusType.Pending && rep.TwinReport.Progress == 0))
                       , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_ResumeZippedStatusFile_UnzipFile()
        {
            var action = initAction();
            action.Action.Unzip = true;
            _fileStreamerWrapperMock.Setup(f => f.GetExtension(action.Action.Source)).Returns(".zip");
            _fileStreamerWrapperMock.Setup(f => f.GetExtension(action.Action.DestinationPath)).Returns("");
            _fileStreamerWrapperMock.Setup(f => f.Combine(It.IsAny<string>(), It.IsAny<string>())).Returns(action.Action.Source);
            _signatureHandlerMock.Setup(sign => sign.VerifyFileSignatureAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            await InitFileDownloadAsync(action);
            action.Report.Status = StatusType.Unzip;
            _fileStreamerWrapperMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            await InitFileDownloadAsync(action);
            _fileStreamerWrapperMock.Verify(x => x.OpenZipFile(It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_CompleteRangeCheckCheckSumInvalid_SendRangeDownloadEvent()
        {
            var action = initAction();
            _checkSumServiceMock.Setup(check => check.CalculateCheckSumAsync(It.IsAny<byte[]>(), It.IsAny<CheckSumType>())).ReturnsAsync("abcd");
            action.Report.CompletedRanges = "0-5,7";
            await InitFileDownloadAsync(action);
            var rangeStartPosition = 123;
            var rangeEndPosition = 456;
            var message = new DownloadBlobChunkMessage
            {
                ActionIndex = action.ActionReported.ReportIndex,
                FileName = action.Action.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 4096,
                RangeIndex = 6,
                RangeCheckSum = "abcd12",
                RangeStartPosition = rangeStartPosition,
                RangeEndPosition = rangeEndPosition
            };
            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            _d2CMessengerHandlerMock.Verify(mf => mf.SendFileDownloadEventAsync(It.IsAny<CancellationToken>(), It.IsAny<string>(), action.Action.Source, action.ActionReported.ReportIndex, "6", rangeStartPosition, rangeEndPosition), Times.Once);

        }

        [Test]
        public async Task HandleDownloadMessageAsync_CompleteRangeCheckCheckSumValid_UpdateCompletedRanges()
        {
            var action = initAction();
            _checkSumServiceMock.Setup(check => check.CalculateCheckSumAsync(It.IsAny<byte[]>(), It.IsAny<CheckSumType>())).ReturnsAsync("abcd");
            action.Report.CompletedRanges = "0-4,7";
            await InitFileDownloadAsync(action);
            var rangeStartPosition = 123;
            var rangeEndPosition = 456;
            var message = new DownloadBlobChunkMessage
            {
                ActionIndex = action.ActionReported.ReportIndex,
                FileName = action.Action.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 4096,
                RangeIndex = 6,
                RangeCheckSum = "abcd",
                RangeStartPosition = rangeStartPosition,
                RangeEndPosition = rangeEndPosition
            };
            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);

            _twinReportHandlerMock.Verify(
                    x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                    item.Any(rep => rep.TwinReport.CompletedRanges == "0-4,6,7"))
                , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_FullRanges_CompleteDownload()
        {
            var action = initAction();
            _checkSumServiceMock.Setup(check => check.CalculateCheckSumAsync(It.IsAny<byte[]>(), It.IsAny<CheckSumType>())).ReturnsAsync("abcd");
            _signatureHandlerMock.Setup(sign => sign.VerifyFileSignatureAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            action.Report.CompletedRanges = "0-5,7";
            await InitFileDownloadAsync(action);
            var message = new DownloadBlobChunkMessage
            {
                ActionIndex = action.ActionReported.ReportIndex,
                FileName = action.Action.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 4096,
                RangeIndex = 6,
                RangesCount = 8,
                RangeCheckSum = "abcd",
                RangeStartPosition = 1,
                RangeEndPosition = 2
            };
            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            _twinReportHandlerMock.Verify(
                    x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                    item.Any(rep => rep.TwinReport.Status == StatusType.Success))
                , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_NotExistFile_ReturnWithoutReport()
        {
            var action = initAction();
            var message = new DownloadBlobChunkMessage
            {
                ActionIndex = -1,
                FileName = action.Action.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 2048
            };

            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            _twinReportHandlerMock.Verify(x =>
                x.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_CheckMaxSizeStrictModeException_ReportFailure()
        {
            var action = initAction();
            await InitFileDownloadAsync(action);
            _strictModeHandlerMock.Setup(th => th.CheckSizeStrictMode(It.IsAny<TwinActionType>(), It.IsAny<long>(), It.IsAny<string>())).Throws<ArgumentException>();

            var message = new DownloadBlobChunkMessage
            {
                ActionIndex = action.ActionReported.ReportIndex,
                FileName = action.Action.Source,
                FileSize = 2048
            };
            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);
        }


        [Test]
        public async Task InitFileDownloadAsync_NoDestinationPath_ReportFailure()
        {
            var action = initAction();
            action.Action.DestinationPath = "";
            await InitFileDownloadAsync(action);
            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);

        }

        [Test]
        public async Task InitFileDownloadAsync_UnzipNotZipFile_ReportFailure()
        {
            var action = initAction();
            action.Action.Unzip = true;

            _fileStreamerWrapperMock.Setup(f => f.GetExtension(It.IsAny<string>())).Returns(".txt");
            await InitFileDownloadAsync(action);

            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);

        }


        [Test]
        public async Task InitFileDownloadAsync_DestinationPathNotContainsExtention_ReportFailure()
        {
            var action = initAction();

            _fileStreamerWrapperMock.Setup(f => f.GetExtension(It.IsAny<string>())).Returns("");
            await InitFileDownloadAsync(action);
            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);

        }

        [Test]
        public async Task InitFileDownloadAsync_NotExistFileDirectories_CreateSubDirectories()
        {
            var action = initAction();

            _fileStreamerWrapperMock.Setup(f => f.GetExtension(It.IsAny<string>())).Returns(".txt");
            await InitFileDownloadAsync(action);
            _fileStreamerWrapperMock.Verify(
                x => x.CreateDirectory(It.IsAny<string>()), Times.Once);

        }

        [Test]
        public async Task HandleDownloadMessageAsync_MessageWithBackendError_ReportFailure()
        {
            var action = initAction();
            await InitFileDownloadAsync(action);
            _strictModeHandlerMock.Setup(th => th.CheckSizeStrictMode(It.IsAny<TwinActionType>(), It.IsAny<long>(), It.IsAny<string>())).Throws<ArgumentException>();
            var errMsg = "error msg";
            var message = new DownloadBlobChunkMessage
            {
                ActionIndex = action.ActionReported.ReportIndex,
                FileName = action.Action.Source,
                Error = errMsg
            };
            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Failed && rep.TwinReport.ResultText == $"Backend error: {errMsg}"))
            , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task InitFileDownloadAsync_FileExist_ReportBlockedStatus()
        {
            var action = initAction();
            _fileStreamerWrapperMock.Setup(item => item.FileExists(It.IsAny<string>())).Returns(true);
            await InitFileDownloadAsync(action);

            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Blocked))
            , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task InitFileDownloadAsync_ZippedDirectoryExist_ReportBlockedStatus()
        {
            var action = initAction();
            action.Action.Unzip = true;
            _fileStreamerWrapperMock.Setup(f => f.GetExtension(action.Action.Source)).Returns(".zip");
            _fileStreamerWrapperMock.Setup(f => f.GetExtension(action.Action.DestinationPath)).Returns("");
            _fileStreamerWrapperMock.Setup(item => item.DirectoryExists(It.IsAny<string>())).Returns(true);
            await InitFileDownloadAsync(action);

            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Blocked))
            , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task InitFileDownloadAsync_NoDiskSpace_ReportBlockedStatus()
        {
            var action = initAction();
            _fileStreamerWrapperMock.Setup(item => item.FileExists(It.IsAny<string>())).Returns(false);
            _fileStreamerWrapperMock.Setup(item => item.isSpaceOnDisk(It.IsAny<string>(), It.IsAny<long>())).Returns(false);
            await InitFileDownloadAsync(action);

            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Blocked))
            , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task InitFileDownloadAsync_OnAccessDenied_ReportBlockedStatus()
        {
            var action = initAction();
            _fileStreamerWrapperMock.Setup(item => item.FileExists(It.IsAny<string>())).Returns(false);
            _fileStreamerWrapperMock.Setup(item => item.WriteChunkToFileAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>())).ThrowsAsync(new System.UnauthorizedAccessException());
            await InitFileDownloadAsync(action);

            _twinReportHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>()
            , It.IsAny<CancellationToken>()), Times.Never);
        }


        [Test]
        public async Task SaveReportAsync_EndAllDownloads_UpdateKnownIdentities()
        {
            _target.InitDownloadsList();
            _signatureHandlerMock.Setup(sign => sign.VerifyFileSignatureAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(true));

            _fileStreamerWrapperMock.Setup(x => x.GetFullPath("C:\\Downloads")).Returns("C:\\Downloads");
            _fileStreamerWrapperMock.Setup(x => x.GetFullPath("C:\\Downloads\\test.txt")).Returns("C:\\Downloads\\test.txt");

            var action1 = await initActionForUpdateKnownIdentities("test.txt", "C:\\Downloads", StatusType.Success);
            var action2 = await initActionForUpdateKnownIdentities("test2.txt", "C:\\Downloads\\test", StatusType.Failed);


            var message = new DownloadBlobChunkMessage
            {
                ActionIndex = action1.ActionReported.ReportIndex,
                FileName = action1.Action.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 1024,
                RangesCount = 2
            };

            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);

            message.ActionIndex = action2.ActionReported.ReportIndex;
            message.FileName = action2.Action.Source;
            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);

            _serverIdentityHandlerMock.Verify(x => x.UpdateKnownIdentitiesFromCertificatesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task InitDownloadsList_GetActionsNull_ReturnNull()
        {
            Assert.DoesNotThrow(() => _target.InitDownloadsList(), It.IsAny<string>());
        }


        private async Task<FileDownload> initActionForUpdateKnownIdentities(string source, string destinationPath, StatusType status)
        {

            var action = new FileDownload()
            {
                ActionReported = new ActionToReport()
                {
                    ReportIndex = actionIndex++,
                    TwinReport = new TwinActionReported(),
                    TwinAction = new DownloadAction()
                    {
                        Source = source,
                        DestinationPath = destinationPath,
                        Sign = "aaaaaa"
                    }
                }
            };
            action.Report.Status = status;
            action.Report.CompletedRanges = "0,1";
            _target.AddFileDownload(action.ActionReported);
            return action;
        }

        private async Task InitFileDownloadAsync(FileDownload action)
        {
            _target.InitDownloadsList();
            _target.AddFileDownload(action.ActionReported);
            await _target.InitFileDownloadAsync(action.ActionReported, CancellationToken.None);
        }

        [Test]
        public async Task AddFileDownload_ExistFile_ReturnFalse()
        {
            var action = initAction();
            _target.AddFileDownload(action.ActionReported);
            action = initAction();
            _target.AddFileDownload(action.ActionReported);
            var res = _target.AddFileDownload(action.ActionReported);

            Assert.IsFalse(res);
        }

        private async Task SetUpHandleDownloadMessageAsync_SignVerifyFailed()
        {
            var action = initAction();
            _checkSumServiceMock.Setup(check => check.CalculateCheckSumAsync(It.IsAny<byte[]>(), It.IsAny<CheckSumType>())).ReturnsAsync("abcd");
            _signatureHandlerMock.Setup(sign => sign.VerifyFileSignatureAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            action.Report.CompletedRanges = "0-5,7";
            await InitFileDownloadAsync(action);
            var message = new DownloadBlobChunkMessage
            {
                ActionIndex = action.ActionReported.ReportIndex,
                FileName = action.Action.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 4096,
                RangeIndex = 6,
                RangesCount = 8,
                RangeCheckSum = "abcd",
                RangeStartPosition = 1,
                RangeEndPosition = 2
            };
            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
        }
    }
}
