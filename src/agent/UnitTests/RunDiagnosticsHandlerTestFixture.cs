using Moq;
using CloudPillar.Agent.Handlers;
using Shared.Logger;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.Options;
using Shared.Entities.Services;
using CloudPillar.Agent.Entities;
using Microsoft.Azure.Devices.Shared;
using Shared.Enums;

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
    private Mock<FileStream> _fileStreamMock;
    private Mock<ILoggerHandler> _logger;


    [SetUp]
    public void Setup()
    {
        _fileUploaderHandlerMock = new Mock<IFileUploaderHandler>();
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _checkSumServiceMock = new Mock<ICheckSumService>();
        _logger = new Mock<ILoggerHandler>();

        _runDiagnosticsSettingsMock = new Mock<IOptions<RunDiagnosticsSettings>>();
        _runDiagnosticsSettingsMock.Setup(x => x.Value).Returns(new RunDiagnosticsSettings());
        _fileStreamMock = new Mock<FileStream>(MockBehavior.Default, new object[] { "filePath", FileMode.Create });

        CreateTarget();
    }

    [Test]
    public async Task CreateFileAsync_FileExists_FileNotCreared()
    {
        _fileStreamerWrapperMock.Setup(h => h.FileExists(It.IsAny<string>())).Returns(true);
        await _target.CreateFileAsync();
        _fileStreamerWrapperMock.Verify(x => x.GetDirectoryName(It.IsAny<string>()), Times.Never);
    }


    [Test]
    public async Task CreateFileAsync_DirectoryNotExists_CreateDirectory()
    {
        _fileStreamerWrapperMock.Setup(h => h.FileExists(It.IsAny<string>())).Returns(false);
        _fileStreamerWrapperMock.Setup(h => h.DirectoryExists(It.IsAny<string>())).Returns(false);
        await _target.CreateFileAsync();
        _fileStreamerWrapperMock.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task CreateFileAsync_FullProcess_CreateFileWithContent()
    {

        _fileStreamerWrapperMock.Setup(h => h.FileExists(It.IsAny<string>())).Returns(false);

        await _target.CreateFileAsync();
        _fileStreamerWrapperMock.Verify(x => x.WriteAsync(It.IsAny<FileStream>(), It.IsAny<ReadOnlyMemory<Byte>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadFileAsync_FullProcess_NotException()
    {
        async Task SendRequest() => await _target.UploadFileAsync(CancellationToken.None);
        Assert.DoesNotThrowAsync(SendRequest);
    }

    [Test]
    public async Task UploadFileAsync_FullProcess_TrhowException()
    {
        _fileUploaderHandlerMock.Setup(x => x.UploadFilesToBlobStorageAsync(It.IsAny<string>(), It.IsAny<UploadAction>(), It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
        .ThrowsAsync(new Exception());

        Assert.ThrowsAsync<Exception>(async () =>
        {
            await _target.UploadFileAsync(CancellationToken.None);
        });

    }

    [Test]
    public async Task WaitingForResponseAsync_GetResponse_Success()
    {
        var actionId = Guid.NewGuid().ToString();
        var checkSum = "testCheckSum";

        _runDiagnosticsSettingsMock.Setup(x=>x.Value).Returns(new RunDiagnosticsSettings(){ PeriodicResponseWaitSeconds = 1});
        CreateTarget();

        _fileStreamerWrapperMock.Setup(x => x.OpenRead(It.IsAny<string>())).Returns(_fileStreamMock.Object);
        _fileStreamerWrapperMock.Setup(x => x.OpenRead(It.IsAny<string>())).Returns(_fileStreamMock.Object);
        _checkSumServiceMock.Setup(x => x.CalculateCheckSumAsync(It.IsAny<FileStream>(), It.IsAny<CheckSumType>())).ReturnsAsync(checkSum);

        CreateTwinMock(actionId, StatusType.Success);

        var result = await _target.WaitingForResponseAsync(actionId);

        Assert.AreEqual(StatusType.Success, result);
    }

    [Test]
    public async Task WaitingForResponseAsync_Timeout_TrhowException()
    {
        var actionId = Guid.NewGuid().ToString();
        _runDiagnosticsSettingsMock.Setup(x=>x.Value).Returns(new RunDiagnosticsSettings(){ PeriodicResponseWaitSeconds = 1, ResponseTimeoutMinutes = 0});
        CreateTarget();
        CreateTwinMock(actionId, StatusType.Pending);

        Assert.ThrowsAsync<TimeoutException>(async () =>
           {
               await _target.WaitingForResponseAsync(actionId);
           });
    }

    private void CreateTwinMock(string actionId, StatusType statusType)
    {
        var twinChangeSpec = new TwinChangeSpec()
        {
            Patch = new TwinPatch()
            {
                TransitPackage = new List<TwinAction>() { new TwinAction() { ActionId = actionId, Action = TwinActionType.SingularDownload } }.ToArray()
            }
        };
        var twinReportedChangeSpec = new TwinReportedChangeSpec()
        {
            Patch = new TwinReportedPatch()
            {
                TransitPackage = new List<TwinActionReported>() { new TwinActionReported() { Status = statusType } }.ToArray()
            }
        };
        var twin = MockHelper.CreateTwinMock(null, null, twinChangeSpec, twinReportedChangeSpec, new List<TwinReportedCustomProp>());
        _deviceClientWrapperMock.Setup(dc => dc.GetTwinAsync(CancellationToken.None)).ReturnsAsync(twin);
    }

    private void CreateTarget()
    {
        _target = new RunDiagnosticsHandler(_fileUploaderHandlerMock.Object, _runDiagnosticsSettingsMock.Object, _fileStreamerWrapperMock.Object,
         _checkSumServiceMock.Object, _deviceClientWrapperMock.Object, _logger.Object);
    }

}
