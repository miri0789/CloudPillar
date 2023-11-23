using Moq;
using CloudPillar.Agent.Handlers;
using Shared.Logger;
using Shared.Entities.Twin;
using CloudPillar.Agent.Wrappers;
using Microsoft.Extensions.Options;
using Shared.Entities.Services;

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


        _target = new RunDiagnosticsHandler(_fileUploaderHandlerMock.Object, _runDiagnosticsSettingsMock.Object, _fileStreamerWrapperMock.Object,
         _checkSumServiceMock.Object, _deviceClientWrapperMock.Object, _logger.Object);
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
    public async Task CreateFileAsync_FullProcess_FileCreateWithContent()
    {
        var fileStreamMock = new Mock<FileStream>(MockBehavior.Strict, new object[] { "filePath", FileMode.Create });

        _fileStreamerWrapperMock.Setup(h => h.FileExists(It.IsAny<string>())).Returns(false);
        _fileStreamerWrapperMock.Setup(h => h.CreateStream(It.IsAny<string>(), It.IsAny<FileMode>())).Returns(fileStreamMock.Object);
        _fileStreamerWrapperMock.Setup(h => h.SetLength(It.IsAny<FileStream>(), 128 * 1024));

        await _target.CreateFileAsync();
        _fileStreamerWrapperMock.Verify(x => x.WriteAsync(It.IsAny<FileStream>(), It.IsAny<ReadOnlyMemory<Byte>>(), It.IsAny<CancellationToken>()), Times.Once);
    }






}
