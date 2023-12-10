using Moq;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;
using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using Shared.Entities.Services;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
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
        private Mock<ITwinActionsHandler> _twinActionsHandlerMock;

        private Mock<ICheckSumService> _checkSumServiceMock;
        private IFileDownloadHandler _target;
        private StrictModeSettings mockStrictModeSettingsValue = new StrictModeSettings();
        private Mock<IOptions<StrictModeSettings>> mockStrictModeSettings;

        private DownloadAction _downloadAction = new DownloadAction()
        {
            ActionId = Guid.NewGuid().ToString(),
            Source = "file.txt",
            DestinationPath = "C:\\Downloads"
        };
        private ActionToReport _actionToReport;

        [SetUp]
        public void Setup()
        {
            _actionToReport = new ActionToReport()
            {
                ReportIndex = 1,
                TwinReport = new TwinActionReported(),
                TwinAction = _downloadAction
            };
            mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock();
            mockStrictModeSettings = new Mock<IOptions<StrictModeSettings>>();
            mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);

            _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
            _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
            _strictModeHandlerMock = new Mock<IStrictModeHandler>();
            _twinActionsHandlerMock = new Mock<ITwinActionsHandler>();
            _checkSumServiceMock = new Mock<ICheckSumService>();
            _loggerMock = new Mock<ILoggerHandler>();
            _fileStreamerWrapperMock.Setup(f => f.GetExtension(It.IsAny<string>())).Returns(".zip");
            _fileStreamerWrapperMock.Setup(x => x.ReadStream(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<long>()))
                                    .Returns(new byte[0]);

            _target = new FileDownloadHandler(_fileStreamerWrapperMock.Object,
             _d2CMessengerHandlerMock.Object,
             _strictModeHandlerMock.Object,
             _twinActionsHandlerMock.Object,
              _loggerMock.Object,
              _checkSumServiceMock.Object);
        }


        [Test]
        public async Task InitFileDownloadAsync_NewDownload_SendFirmwareUpdateEvent()
        {
            _d2CMessengerHandlerMock.Setup(dc => dc.SendFirmwareUpdateEventAsync(CancellationToken.None, _downloadAction.Source, _downloadAction.ActionId, 0, null, null));

            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);

            _d2CMessengerHandlerMock.Verify(mf => mf.SendFirmwareUpdateEventAsync(CancellationToken.None, _downloadAction.Source, _downloadAction.ActionId, 0, null, null), Times.Once);

        }

        [Test]
        public async Task InitFileDownloadAsync_ActiveDownload_SendFirmwareUpdateEventWithRange()
        {
            _actionToReport.TwinReport.CompletedRanges = "0-5,8,10";
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);

            _d2CMessengerHandlerMock.Verify(mf => mf.SendFirmwareUpdateEventAsync(CancellationToken.None, _downloadAction.Source, _downloadAction.ActionId, 6, null, null), Times.Once);

        }

        [Test]
        public async Task InitFileDownloadAsync_SendFirmwareEventFailure_UpdateReportedToFaild()
        {
            _d2CMessengerHandlerMock.Setup(dc =>
                    dc.SendFirmwareUpdateEventAsync(It.IsAny<CancellationToken>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<long?>(), It.IsAny<long?>()))
                    .ThrowsAsync(new Exception());
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);

            _twinActionsHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item => item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_PartiallyData_ReportInprogressWithProgress()
        {
            _actionToReport.TwinAction.ActionId = Guid.NewGuid().ToString();
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);

            var message = new DownloadBlobChunkMessage
            {
                ActionId = _actionToReport.TwinAction.ActionId,
                FileName = _downloadAction.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 4096
            };

            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);


            _twinActionsHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinAction.ActionId == _actionToReport.TwinAction.ActionId && rep.TwinReport.Status == StatusType.InProgress && rep.TwinReport.Progress == 25))
            , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_Failure_DeleteFile()
        {
            _actionToReport.TwinAction.ActionId = Guid.NewGuid().ToString();
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);

            var message = new DownloadBlobChunkMessage
            {
                ActionId = _actionToReport.TwinAction.ActionId,
                FileName = _downloadAction.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 4096
            };
            _fileStreamerWrapperMock.Setup(f => f.WriteChunkToFileAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>())).ThrowsAsync(new Exception());

            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);


            _fileStreamerWrapperMock.Verify(f => f.DeleteFile(It.IsAny<string>()), Times.Once);
        }


        [Test]
        public async Task HandleDownloadMessageAsync_InProgressDeletedFile_InitProgress()
        {
            _actionToReport.TwinAction.ActionId = Guid.NewGuid().ToString();
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);
            _actionToReport.TwinReport.Progress = 50;
            _actionToReport.TwinReport.Status = StatusType.InProgress;
            _fileStreamerWrapperMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(false);
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);
            _twinActionsHandlerMock.Verify(
                           x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                           item.Any(rep => rep.TwinReport.Status == StatusType.Pending && rep.TwinReport.Progress == 0))
                       , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_InProgressZippedStatusFile_UnzipFile()
        {
            _actionToReport.TwinAction = new DownloadAction()
            {
                ActionId = Guid.NewGuid().ToString(),
                Source = _downloadAction.Source,
                DestinationPath = _downloadAction.DestinationPath,
                Unzip = true
            };
            _actionToReport.TwinReport.Status = StatusType.Unzip;
            _fileStreamerWrapperMock.Setup(f => f.FileExists(It.IsAny<string>())).Returns(true);
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);
            _fileStreamerWrapperMock.Verify(x => x.UnzipFileAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }


        [Test]
        public async Task HandleDownloadMessageAsync_CompleteRangeCheckCheckSumInvalid_SendRangeFirmwareEvent()
        {
            var actionId = Guid.NewGuid().ToString();
            _actionToReport.TwinAction = new DownloadAction()
            {
                ActionId = actionId,
                Source = _downloadAction.Source,
                DestinationPath = _downloadAction.DestinationPath,
            };
            _checkSumServiceMock.Setup(check => check.CalculateCheckSumAsync(It.IsAny<byte[]>(), It.IsAny<CheckSumType>())).ReturnsAsync("abcd");
            _actionToReport.TwinReport.CompletedRanges = "0-5,7";
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);
            var rangeStartPosition = 123;
            var rangeEndPosition = 456;
            var message = new DownloadBlobChunkMessage
            {
                ActionId = actionId,
                FileName = _downloadAction.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 4096,
                RangeIndex = 6,
                RangeCheckSum = "abcd12",
                RangeStartPosition = rangeStartPosition,
                RangeEndPosition = rangeEndPosition
            };
            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            _d2CMessengerHandlerMock.Verify(mf => mf.SendFirmwareUpdateEventAsync(It.IsAny<CancellationToken>(), _downloadAction.Source, actionId, 0, rangeStartPosition, rangeEndPosition), Times.Once);

        }

        [Test]
        public async Task HandleDownloadMessageAsync_CompleteRangeCheckCheckSumValid_UpdateCompletedRanges()
        {
            var actionId = Guid.NewGuid().ToString();
            _actionToReport.TwinAction = new DownloadAction()
            {
                ActionId = actionId,
                Source = _downloadAction.Source,
                DestinationPath = _downloadAction.DestinationPath,
            };
            _checkSumServiceMock.Setup(check => check.CalculateCheckSumAsync(It.IsAny<byte[]>(), It.IsAny<CheckSumType>())).ReturnsAsync("abcd");
            _actionToReport.TwinReport.CompletedRanges = "0-4,7";
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);
            var rangeStartPosition = 123;
            var rangeEndPosition = 456;
            var message = new DownloadBlobChunkMessage
            {
                ActionId = _actionToReport.TwinAction.ActionId,
                FileName = _downloadAction.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 4096,
                RangeIndex = 6,
                RangeCheckSum = "abcd",
                RangeStartPosition = rangeStartPosition,
                RangeEndPosition = rangeEndPosition
            };
            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);

            _twinActionsHandlerMock.Verify(
                    x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                    item.Any(rep => rep.TwinReport.CompletedRanges == "0-4,6,7"))
                , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_FullRanges_CompleteDownload()
        {
            var actionId = Guid.NewGuid().ToString();
            _actionToReport.TwinAction = new DownloadAction()
            {
                ActionId = actionId,
                Source = _downloadAction.Source,
                DestinationPath = _downloadAction.DestinationPath,
            };
            _checkSumServiceMock.Setup(check => check.CalculateCheckSumAsync(It.IsAny<byte[]>(), It.IsAny<CheckSumType>())).ReturnsAsync("abcd");
            _actionToReport.TwinReport.CompletedRanges = "0-5,7";
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);
            var message = new DownloadBlobChunkMessage
            {
                ActionId = _actionToReport.TwinAction.ActionId,
                FileName = _downloadAction.Source,
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
            _twinActionsHandlerMock.Verify(
                    x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                    item.Any(rep => rep.TwinReport.Status == StatusType.Success))
                , It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_NotExistFile_ReturnWithoutReport()
        {
            var message = new DownloadBlobChunkMessage
            {
                ActionId = "NotExistActionId",
                FileName = _downloadAction.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 2048
            };

            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            _twinActionsHandlerMock.Verify(x =>
                x.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_CheckMaxSizeStrictModeException_ReportFailure()
        {
            var actionId = Guid.NewGuid().ToString();
            _actionToReport.TwinAction = new DownloadAction()
            {
                ActionId = actionId,
                Source = _downloadAction.Source,
                DestinationPath = _downloadAction.DestinationPath,
            };
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);
            _strictModeHandlerMock.Setup(th => th.CheckSizeStrictMode(It.IsAny<TwinActionType>(), It.IsAny<long>(), It.IsAny<string>())).Throws<ArgumentException>();

            var message = new DownloadBlobChunkMessage
            {
                ActionId = actionId,
                FileName = _downloadAction.Source,
                FileSize = 2048
            };
            await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            _twinActionsHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);
        }




        [Test]
        public async Task HandleDownloadMessageAsync_NoDestinationPath_ReportFailure()
        {
            _actionToReport.TwinAction = new DownloadAction()
            {
                ActionId = Guid.NewGuid().ToString(),
                Source = "file.txt",
                DestinationPath = null,
                Unzip = true
            };
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);
            _twinActionsHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);

        }

        [Test]
        public async Task HandleDownloadMessageAsync_DestinationPathIsNotFolder_ReportFailure()
        {
            _actionToReport.TwinAction = new DownloadAction()
            {
                ActionId = Guid.NewGuid().ToString(),
                Source = "file.zip",
                DestinationPath = "C:\\Downloads\\test.zip",
                Unzip = true
            };

            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);
            _twinActionsHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);

        }

        [Test]
        public async Task HandleDownloadMessageAsync_UnzipNotZipFile_ReportFailure()
        {
            _actionToReport.TwinAction = new DownloadAction()
            {
                ActionId = Guid.NewGuid().ToString(),
                Source = "file.txt",
                DestinationPath = "C:\\Downloads",
                Unzip = true
            };

            _fileStreamerWrapperMock.Setup(f => f.GetExtension(It.IsAny<string>())).Returns(".txt");
            await _target.InitFileDownloadAsync(_actionToReport, CancellationToken.None);

            _twinActionsHandlerMock.Verify(
                x => x.UpdateReportActionAsync(It.Is<IEnumerable<ActionToReport>>(item =>
                item.Any(rep => rep.TwinReport.Status == StatusType.Failed))
            , It.IsAny<CancellationToken>()), Times.Once);

        }
    }
}
