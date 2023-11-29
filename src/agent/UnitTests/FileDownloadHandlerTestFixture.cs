using Moq;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;
using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using Shared.Logger;
using Microsoft.Extensions.Options;

namespace CloudPillar.Agent.Tests
{
    [TestFixture]
    public class FileDownloadHandlerTestFixture
    {
        private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
        private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
        private Mock<IStrictModeHandler> _strictModeHandlerMock;
        private Mock<IRunDiagnosticsHandler> _runDiagnosticsHandlerMock;
        private Mock<ILoggerHandler> _loggerMock;
        private Mock<ITwinActionsHandler> _twinActionsHandlerMock;
        private IFileDownloadHandler _target;
        private StrictModeSettings mockStrictModeSettingsValue = new StrictModeSettings();
        private Mock<IOptions<StrictModeSettings>> mockStrictModeSettings;

        private DownloadAction _downloadAction = new DownloadAction()
        {
            ActionId = "action123",
            Source = "file.txt",
            DestinationPath = "C:\\Downloads"
        };
        private ActionToReport _actionToReport = new ActionToReport()
        {
            ReportIndex = 1,
            TwinReport = new TwinActionReported()
        };

        [SetUp]
        public void Setup()
        {
            mockStrictModeSettingsValue = StrictModeMockHelper.SetStrictModeSettingsValueMock();
            mockStrictModeSettings = new Mock<IOptions<StrictModeSettings>>();
            mockStrictModeSettings.Setup(x => x.Value).Returns(mockStrictModeSettingsValue);

            _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
            _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
            _strictModeHandlerMock = new Mock<IStrictModeHandler>();
            _runDiagnosticsHandlerMock = new Mock<IRunDiagnosticsHandler>();
            _twinActionsHandlerMock = new Mock<ITwinActionsHandler>();
            _loggerMock = new Mock<ILoggerHandler>();

            _target = new FileDownloadHandler(_fileStreamerWrapperMock.Object,
             _d2CMessengerHandlerMock.Object,
             _strictModeHandlerMock.Object,
             _runDiagnosticsHandlerMock.Object,
             _twinActionsHandlerMock.Object,
              _loggerMock.Object);
        }


        [Test]
        public async Task InitFileDownloadAsync_Add_SendFirmwareUpdateEvent()
        {
            _d2CMessengerHandlerMock.Setup(dc => dc.SendFirmwareUpdateEventAsync(_downloadAction.Source, _downloadAction.ActionId, null, null));

            await _target.InitFileDownloadAsync(_downloadAction, _actionToReport);

            _d2CMessengerHandlerMock.Verify(mf => mf.SendFirmwareUpdateEventAsync(_downloadAction.Source, _downloadAction.ActionId, null, null), Times.Once);

        }

        [Test]
        public async Task InitFileDownloadAsync_Failure_ThrowException()
        {
            _d2CMessengerHandlerMock.Setup(dc =>
                    dc.SendFirmwareUpdateEventAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<long?>()))
                    .ThrowsAsync(new Exception());

            Assert.ThrowsAsync<Exception>(async () =>
            {
                await _target.InitFileDownloadAsync(_downloadAction, _actionToReport);
            });

        }

        [Test]
        public async Task HandleDownloadMessageAsync_PartiallyData_ReturnInprogressReport()
        {
            await _target.InitFileDownloadAsync(_downloadAction, _actionToReport);

            var message = new DownloadBlobChunkMessage
            {
                ActionId = _downloadAction.ActionId,
                FileName = _downloadAction.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 2048
            };
            _fileStreamerWrapperMock.Setup(f => f.WriteChunkToFileAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>()));

            var report = await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            Assert.AreEqual((report.TwinReport.Status, report.TwinReport.Progress), (StatusType.InProgress, 50));
        }

        [Test]
        public async Task HandleDownloadMessageAsync_AllFileBytes_ReturnSuccessReport()
        {
            await _target.InitFileDownloadAsync(_downloadAction, _actionToReport);

            var message = new DownloadBlobChunkMessage
            {
                ActionId = _downloadAction.ActionId,
                FileName = _downloadAction.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 1024
            };
            _fileStreamerWrapperMock.Setup(f => f.WriteChunkToFileAsync(It.IsAny<string>(), It.IsAny<long>(), It.IsAny<byte[]>()));

            var report = await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            Assert.AreEqual(report.TwinReport.Status, StatusType.Success);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_NotExistFile_ThrowException()
        {
            var message = new DownloadBlobChunkMessage
            {
                ActionId = "NotExistActionId",
                FileName = _downloadAction.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 2048
            };

            Assert.ThrowsAsync<ArgumentException>(async () =>
                   {
                       await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
                   });
        }

        [Test]
        public async Task HandleDownloadMessageAsync_PassStrictMode_Success()
        {
            var _downloadActionForSM = new DownloadAction()
            {
                ActionId = "action123",
                Source = "test.txt",
                DestinationPath = $"${{{StrictModeMockHelper.DOWNLOAD_KEY}}}"
            };
            await _target.InitFileDownloadAsync(_downloadActionForSM, _actionToReport);

            var message = new DownloadBlobChunkMessage
            {
                ActionId = _downloadActionForSM.ActionId,
                FileName = _downloadActionForSM.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 2048
            };

            var report = await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            Assert.AreNotEqual(report.TwinReport.Status, StatusType.Failed);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_MaxSizeStrictMode_ThrowException()
        {
            var _downloadActionForSM = new DownloadAction()
            {
                ActionId = "action123",
                Source = "test.txt",
                DestinationPath = $"${{{StrictModeMockHelper.DOWNLOAD_KEY}}}",
            };
            await _target.InitFileDownloadAsync(_downloadActionForSM, _actionToReport);
            _strictModeHandlerMock.Setup(th => th.CheckSizeStrictMode(It.IsAny<TwinActionType>(), It.IsAny<long>(), It.IsAny<string>())).Throws<ArgumentException>();

            var message = new DownloadBlobChunkMessage
            {
                ActionId = _downloadActionForSM.ActionId,
                FileName = _downloadActionForSM.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 2048
            };

            var report = await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            Assert.AreEqual(report.TwinReport.Status, StatusType.Failed);
        }

        [Test]
        public async Task HandleDownloadMessageAsync_NoRootForId_ThrowException()
        {
            mockStrictModeSettingsValue.FilesRestrictions.First(x => x.Type == StrictModeMockHelper.DOWNLOAD).Root = "";

            var message = new DownloadBlobChunkMessage
            {
                ActionId = "abc",
                FileName = $"${{{StrictModeMockHelper.DOWNLOAD_KEY}}}test.txt",
                Offset = 0,
                Data = new byte[1024],
                FileSize = 2048
            };

            Assert.ThrowsAsync<ArgumentException>(async () =>
                           {
                               await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
                           });
        }


        [Test]
        public async Task HandleDownloadMessageAsync_UnzipNotZipFile_ReturnInProgressReport()
        {
            var _downloadAction2 = new DownloadAction()
            {
                ActionId = "action123",
                Source = "file.txt",
                DestinationPath = "C:\\Downloads",
                Unzip = true
            };

            await _target.InitFileDownloadAsync(_downloadAction2, _actionToReport);

            var message = new DownloadBlobChunkMessage
            {
                ActionId = _downloadAction2.ActionId,
                FileName = _downloadAction2.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 2048
            };
            _fileStreamerWrapperMock.Setup(f => f.GetExtension(It.IsAny<string>())).Returns(".txt");
            var report = await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            Assert.AreEqual(report.TwinReport.Status, StatusType.InProgress);

        }
        [Test]
        public async Task HandleDownloadMessageAsync_NoDestinationPath_ReturnFailedReport()
        {           
            var _downloadAction2 = new DownloadAction()
            {
                ActionId = "action123",
                Source = "file.txt",
                DestinationPath = null,
                Unzip = true
            };

            await _target.InitFileDownloadAsync(_downloadAction2, _actionToReport);

            var message = new DownloadBlobChunkMessage
            {
                ActionId = _downloadAction2.ActionId,
                FileName = _downloadAction2.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 2048
            };

            var report = await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            Assert.AreEqual(report.TwinReport.Status, StatusType.Failed);

        }

        [Test]
        public async Task HandleDownloadMessageAsync_UnzipToFile_ReturnFailedReport()
        {
            var _downloadAction2 = new DownloadAction()
            {
                ActionId = "action123",
                Source = "file.txt",
                DestinationPath = "C:\\Downloads\\test.zip",
                Unzip = true
            };

            await _target.InitFileDownloadAsync(_downloadAction2, _actionToReport);

            var message = new DownloadBlobChunkMessage
            {
                ActionId = _downloadAction2.ActionId,
                FileName = _downloadAction2.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 2048
            };
            _fileStreamerWrapperMock.Setup(f => f.GetExtension(It.IsAny<string>())).Returns(".zip");
            var report = await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            Assert.AreEqual(report.TwinReport.Status, StatusType.Failed);

        }
        [Test]
        public async Task HandleDownloadMessageAsync_DestinationPathDirectoryNotExists_ReturnInProgressReport()
        {
           var _downloadAction2 = new DownloadAction()
            {
                ActionId = "action123",
                Source = "file.zip",
                DestinationPath = "C:\\Downloads",
                Unzip = true
            };

            await _target.InitFileDownloadAsync(_downloadAction2, _actionToReport);

            var message = new DownloadBlobChunkMessage
            {
                ActionId = _downloadAction2.ActionId,
                FileName = _downloadAction2.Source,
                Offset = 0,
                Data = new byte[1024],
                FileSize = 2048
            };
            _fileStreamerWrapperMock.Setup(f => f.GetExtension(_downloadAction2.Source)).Returns(".zip");
            _fileStreamerWrapperMock.Setup(f => f.GetExtension(_downloadAction2.DestinationPath)).Returns(string.Empty);
            _fileStreamerWrapperMock.Setup(f => f.DirectoryExists(_downloadAction2.Source)).Returns(true);
            _fileStreamerWrapperMock.Setup(f => f.DirectoryExists(_downloadAction2.DestinationPath)).Returns(false);
            var report = await _target.HandleDownloadMessageAsync(message, CancellationToken.None);
            Assert.AreEqual(report.TwinReport.Status, StatusType.InProgress);

        }
    }
}
