using Moq;
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.Options;
using Shared.Entities.Services;
using CloudPillar.Agent.Entities;
using Shared.Enums;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Wrappers.Interfaces;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class RunDiagnosticsHandlerTestFixture
{
    private IRunDiagnosticsHandler _target;

    private Mock<IFileUploaderHandler> _fileUploaderHandlerMock;
    private Mock<IOptions<RunDiagnosticsSettings>> _runDiagnosticsSettingsMock;
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private Mock<ICheckSumService> _checkSumServiceMock;
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<IGuidWrapper> _guidWrapperMock;
    private Mock<FileStream> _fileStreamMock;
    private Mock<ILoggerHandler> _logger;
    private const int FILE_SIZE_BYTES = 128 * 1024;
    private const string uploadFilePath = "uploadFilePath";
    private const string checkSum = "testCheckSum";
    private const string DIAGNOSTICS_EXTENSION = ".tmp";
    private string guid = Guid.NewGuid().ToString();
    private string filePath = string.Empty;

    private CancellationToken cancellationToken = CancellationToken.None;


    [SetUp]
    public void Setup()
    {
        filePath = Path.Combine(uploadFilePath, $"{guid}{DIAGNOSTICS_EXTENSION}");

        _fileUploaderHandlerMock = new Mock<IFileUploaderHandler>();
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _checkSumServiceMock = new Mock<ICheckSumService>();
        _guidWrapperMock = new Mock<IGuidWrapper>();
        _logger = new Mock<ILoggerHandler>();

        _runDiagnosticsSettingsMock = new Mock<IOptions<RunDiagnosticsSettings>>();
        _runDiagnosticsSettingsMock.Setup(x => x.Value).Returns(new RunDiagnosticsSettings() { FileSizeBytes = FILE_SIZE_BYTES, PeriodicResponseWaitSeconds = 2, ResponseTimeoutMinutes = 1 });
        _fileStreamMock = new Mock<FileStream>(MockBehavior.Default, new object[] { "filePath", FileMode.Create });

        _guidWrapperMock.Setup(x => x.CreateNewGuid()).Returns(guid);
        _fileStreamerWrapperMock.Setup(x => x.GetTempPath()).Returns(uploadFilePath);
        _fileStreamerWrapperMock.Setup(f => f.CreateStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>(), It.IsAny<int>(), It.IsAny<bool>()))
        .Returns(() => _fileStreamMock.Object);
        _fileStreamerWrapperMock.Setup(x => x.GetFileName(It.IsAny<string>())).Returns(guid);
        _checkSumServiceMock.Setup(x => x.CalculateCheckSumAsync(It.IsAny<FileStream>(), It.IsAny<CheckSumType>())).ReturnsAsync(checkSum);
        InitTwin(StatusType.Success);
        CreateTarget();
    }


    [Test]
    public async Task UploadFileAsync_FullProcess_Success()
    {
        var reported = await _target.HandleRunDiagnosticsProcess(CancellationToken.None);
        Assert.AreEqual(StatusType.Success, reported.Status);
    }

    [Test]
    public async Task UploadFileAsync_DownloadFialed_Failed()
    {
        InitTwin(StatusType.Failed);
        var reported = await _target.HandleRunDiagnosticsProcess(CancellationToken.None);
        Assert.AreEqual(StatusType.Failed, reported.Status);
    }
    [Test]
    public async Task CreateFileAsync_FillRandomBytes_WriteContentToFile()
    {
        await _target.HandleRunDiagnosticsProcess(CancellationToken.None);
        _fileStreamerWrapperMock.Verify(x => x.WriteAsync(It.IsAny<Stream>(), It.IsAny<byte[]>()), Times.Once);
    }

    [Test]
    public async Task CreateFileAsync_SetLengthFile_FileSizeFromSettings()
    {
        await _target.HandleRunDiagnosticsProcess(CancellationToken.None);
        _fileStreamerWrapperMock.Verify(
                    x => x.SetLength(It.IsAny<Stream>(), _runDiagnosticsSettingsMock.Object.Value.FileSizeBytes), Times.Once);
    }

    [Test]
    public async Task UploadFileAsync_BuilUploadAction_UploadFile()
    {
        var uploadAction = new UploadAction()
        {
            Action = TwinActionType.SingularUpload,
            Description = "upload file by run diagnostic",
            Method = FileUploadMethod.Stream,
            FileName = filePath
        };
        await _target.HandleRunDiagnosticsProcess(CancellationToken.None);


        _fileUploaderHandlerMock.Verify(
         x => x.UploadFilesToBlobStorageAsync(It.IsAny<ActionToReport>(), FileUploadMethod.Stream, filePath, It.IsAny<string>(), It.IsAny<CancellationToken>(), It.Is<bool>(x => x == true)), Times.Once);

    }

    [Test]
    public async Task CheckDownloadStatus_TimeOut_ThrowException()
    {
        InitTwin(StatusType.Pending);
        _runDiagnosticsSettingsMock.Setup(x => x.Value).Returns(new RunDiagnosticsSettings() { PeriodicResponseWaitSeconds = 1, ResponseTimeoutMinutes = 0 });
        CreateTarget();

        Assert.ThrowsAsync<TimeoutException>(async () =>
            {
                await _target.HandleRunDiagnosticsProcess(CancellationToken.None);
            });

    }

    [Test]
    public async Task CheckDownloadStatus_NotEqualFiles_Failed()
    {
        _fileStreamerWrapperMock.Setup(x => x.OpenRead(It.IsAny<string>())).Returns(_fileStreamMock.Object);
        _checkSumServiceMock.Setup(x => x.CalculateCheckSumAsync(_fileStreamMock.Object, It.IsAny<CheckSumType>())).ReturnsAsync(_fileStreamMock.Object.Name);
        CreateTarget();
        InitTwin(StatusType.Success);
        var reported = await _target.HandleRunDiagnosticsProcess(CancellationToken.None);
        Assert.AreEqual(StatusType.Failed, reported.Status);

    }

    [Test]
    public async Task CheckDownloadStatus_FullProcess_DeletTempFiles()
    {
        _fileStreamerWrapperMock.Setup(x => x.FileExists(It.IsAny<string>())).Returns(true);
        InitTwin(StatusType.Success);
        var reported = await _target.HandleRunDiagnosticsProcess(CancellationToken.None);
        _fileStreamerWrapperMock.Verify(x => x.DeleteFile(It.IsAny<string>()), Times.Exactly(2));

    }

    private void InitTwin(StatusType statusType)
    {
        var changeSign = "changeSign";
        var desired = new TwinDesired()
        {
            ChangeSpecDiagnostics = new TwinChangeSpec()
            {
                Id = "123",
                Patch = new Dictionary<string, TwinAction[]>{
                    {"TransitPackage", new List<TwinAction>()
                    {
                        new DownloadAction() {
                            Action = TwinActionType.SingularDownload,
                            DestinationPath =  guid,
                            Source = guid
                         }
                    }.ToArray() }
                }
            }
        };

        var reported = new TwinReported()
        {
            ChangeSpecDiagnostics = new TwinReportedChangeSpec()
            {
                Id = "123",
                Patch = new Dictionary<string, TwinActionReported[]>{
                    {"TransitPackage", new TwinActionReported[]{ new TwinActionReported() { Status = statusType} } }
                }
            }
        };
        var twin = MockHelper.CreateTwinMock(new TwinChangeSpec(), new TwinReportedChangeSpec(), desired.ChangeSpecDiagnostics, reported.ChangeSpecDiagnostics, null, changeSign);
        _deviceClientWrapperMock.Setup(dc => dc.GetTwinAsync(cancellationToken)).ReturnsAsync(twin);

    }

    private void CreateTarget()
    {
        _target = new RunDiagnosticsHandler(_fileUploaderHandlerMock.Object, _runDiagnosticsSettingsMock.Object, _fileStreamerWrapperMock.Object,
         _checkSumServiceMock.Object, _deviceClientWrapperMock.Object, _guidWrapperMock.Object, _logger.Object);
    }

}
