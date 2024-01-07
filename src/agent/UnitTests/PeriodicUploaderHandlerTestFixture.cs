using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers;
using Moq;
using Shared.Entities.Services;
using Shared.Entities.Twin;
using Shared.Enums;

[TestFixture]
public class PeriodicUploaderHandlerTestFixture
{
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private Mock<IFileUploaderHandler> _fileUploaderHandlerMock;
    private Mock<ICheckSumService> _checkSumServiceMock;
    private Mock<ITwinReportHandler> _twinReportHandlerMock;
    private IPeriodicUploaderHandler _target;
    private ActionToReport _actionToReport;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new Mock<ILoggerHandler>();
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
        _fileUploaderHandlerMock = new Mock<IFileUploaderHandler>();
        _checkSumServiceMock = new Mock<ICheckSumService>();
        _twinReportHandlerMock = new Mock<ITwinReportHandler>();
        _actionToReport = new ActionToReport
        {
            TwinAction = new PeriodicUploadAction
            {
                DirName = "testDirName"
            },
            TwinReport = new TwinActionReported()
        };

        _fileStreamerWrapperMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileStreamerWrapperMock.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(new string[] { "testFileName" });
        _fileUploaderHandlerMock.Setup(x => x.FileUploadAsync(It.IsAny<ActionToReport>(), It.IsAny<FileUploadMethod>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _twinReportHandlerMock.Setup(x => x.GetPeriodicReportedKey(It.IsAny<PeriodicUploadAction>(), It.IsAny<string>())).Returns("testKey");
        _checkSumServiceMock.Setup(x => x.CalculateCheckSumAsync(It.IsAny<Stream>(), It.IsAny<CheckSumType>())).ReturnsAsync("testCheckSum");
        _twinReportHandlerMock.Setup(x => x.GetActionToReport(It.IsAny<ActionToReport>(), It.IsAny<string>())).Returns(new TwinActionReported() { CheckSum = "testCheckSum2" });

        _target = new PeriodicUploaderHandler(_loggerMock.Object, _fileStreamerWrapperMock.Object, _fileUploaderHandlerMock.Object, _checkSumServiceMock.Object, _twinReportHandlerMock.Object);
    }

    [Test]
    public async Task UploadAsync_WhenDirectoryAndFileNotExist_ShouldReportFailed()
    {
        _fileStreamerWrapperMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);
        _fileStreamerWrapperMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(false);

        // Act
        await _target.UploadAsync(_actionToReport, "testChangeSpecId", CancellationToken.None);
        // Assert        
        _twinReportHandlerMock.Verify(
            x => x.SetReportProperties(It.IsAny<ActionToReport>(), It.Is<StatusType>(item => item == StatusType.Success),
             It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task UploadAsync_WhenDirectoryNotExist_ShouldSetReportProperties()
    {
        _fileStreamerWrapperMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);
        _fileStreamerWrapperMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);

        // Act
        await _target.UploadAsync(_actionToReport, "testChangeSpecId", CancellationToken.None);
        // Assert
        _fileStreamerWrapperMock.Verify(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>()), Times.Never);
    }

    [Test]
    public async Task UploadAsync_WhenDirectoryIsEmpty_ShouldNotUpload()
    {
        _fileStreamerWrapperMock.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(new string[] { });

        // Act
        await _target.UploadAsync(_actionToReport, "testChangeSpecId", CancellationToken.None);
        // Assert
        _fileUploaderHandlerMock.Verify(x => x.FileUploadAsync(It.IsAny<ActionToReport>(), It.IsAny<FileUploadMethod>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UploadAsync_OnCall_DoFinally()
    {
        // Act
        await _target.UploadAsync(_actionToReport, "testChangeSpecId", CancellationToken.None);
        // Assert
        _twinReportHandlerMock.Verify(x => x.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Test]
    public async Task UploadAsync_WhenDirectoryIsNotEmpty_ShouldUpload()
    {
        // Act
        await _target.UploadAsync(_actionToReport, "testChangeSpecId", CancellationToken.None);
        // Assert
        _fileUploaderHandlerMock.Verify(x => x.FileUploadAsync(It.IsAny<ActionToReport>(), It.IsAny<FileUploadMethod>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadAsync_WhenFileUpToDate_ShouldNotUpload()
    {
        _twinReportHandlerMock.Setup(x => x.GetActionToReport(It.IsAny<ActionToReport>(), It.IsAny<string>())).Returns(new TwinActionReported() { CheckSum = "testCheckSum" });
        // Act
        await _target.UploadAsync(_actionToReport, "testChangeSpecId", CancellationToken.None);
        // Assert
        _fileUploaderHandlerMock.Verify(x => x.FileUploadAsync(It.IsAny<ActionToReport>(), It.IsAny<FileUploadMethod>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UploadAsync_WhenIntervalIsZero_StatusSuccess()
    {
        // Act
        await _target.UploadAsync(_actionToReport, "testChangeSpecId", CancellationToken.None);
        // Assert
        _twinReportHandlerMock.Verify(x => x.SetReportProperties(It.IsAny<ActionToReport>(), It.Is<StatusType>(item => item == StatusType.Success), null, "Interval is empty", ""), Times.Once);
    }


    [Test]
    public async Task UploadAsync_WhenIntervalIsNotZero_StatusIdle()
    {
        _actionToReport.TwinAction = new PeriodicUploadAction
        {
            DirName = "testDirName",
            Interval = 1
        };

        // Act
        await _target.UploadAsync(_actionToReport, "testChangeSpecId", CancellationToken.None);
        // Assert
        _twinReportHandlerMock.Verify(x => x.SetReportProperties(It.IsAny<ActionToReport>(), It.Is<StatusType>(item => item == StatusType.Idle), null, null, ""), Times.Once);
    }


    [Test]
    public async Task UploadAsync_OnError_StatusSuccess()
    {
        _twinReportHandlerMock.Setup(x => x.GetActionToReport(It.IsAny<ActionToReport>(), It.IsAny<string>())).Returns((TwinActionReported)null);
        // Act
        await _target.UploadAsync(_actionToReport, "testChangeSpecId", CancellationToken.None);
        // Assert
        _twinReportHandlerMock.Verify(x => x.SetReportProperties(It.IsAny<ActionToReport>(), It.Is<StatusType>(item => item == StatusType.Failed), It.IsAny<string>(), It.IsAny<string>(), ""), Times.Once);
    }
}