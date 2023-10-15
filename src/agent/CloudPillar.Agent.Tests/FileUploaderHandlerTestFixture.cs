using Moq;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;
using CloudPillar.Agent.Entities;
using Shared.Logger;
using Microsoft.Azure.Devices.Client.Transport;

[TestFixture]
public class FileUploaderHandlerTestFixture
{
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<IBlobStorageFileUploaderHandler> _blobStorageFileUploaderHandlerMock;
    private Mock<IStreamingFileUploaderHandler> _streamingFileUploaderHandlerMock;
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private Mock<ITwinActionsHandler> _twinActionsHandler;
    private Mock<ILoggerHandler> _loggerMock;
    private IFileUploaderHandler _target;
    private MemoryStream READ_STREAM = new MemoryStream(new byte[] { 1, 2, 3 });
    private readonly Uri STORAGE_URI = new Uri("https://mockstorage.example.com/mock-container");
    FileUploadSasUriResponse sasUriResponse = new FileUploadSasUriResponse();
    private UploadAction uploadAction = new UploadAction
    {
        FileName = "testFileName"
    };

    private ActionToReport actionToReport = new ActionToReport
    {
        TwinReport = new TwinActionReported()
    };

    [SetUp]
    public void Setup()
    {
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _blobStorageFileUploaderHandlerMock = new Mock<IBlobStorageFileUploaderHandler>();
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
        _streamingFileUploaderHandlerMock = new Mock<IStreamingFileUploaderHandler>();
        _twinActionsHandler = new Mock<ITwinActionsHandler>();
        _loggerMock = new Mock<ILoggerHandler>();

        _deviceClientWrapperMock.Setup(device => device.GetFileUploadSasUriAsync(It.IsAny<FileUploadSasUriRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(sasUriResponse);

        _deviceClientWrapperMock.Setup(device => device.GetBlobUriAsync(It.IsAny<FileUploadSasUriResponse>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(STORAGE_URI);

        _fileStreamerWrapperMock.Setup(f => f.CreateStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>(), It.IsAny<int>(), It.IsAny<bool>()))
        .Returns(() => READ_STREAM);
        _fileStreamerWrapperMock.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new string[] { });
        _fileStreamerWrapperMock.Setup(x => x.GetDirectories(It.IsAny<string>(), It.IsAny<string>())).Returns(new string[] { });

        _target = new FileUploaderHandler(_deviceClientWrapperMock.Object, _fileStreamerWrapperMock.Object, _blobStorageFileUploaderHandlerMock.Object, _streamingFileUploaderHandlerMock.Object,
        _twinActionsHandler.Object, _loggerMock.Object);


    }


    [Test]
    public async Task UploadFilesToBlobStorageAsync_ValidStorageFiles_ReturnStatusSuccess()
    {
        _fileStreamerWrapperMock.Setup(x => x.Concat(It.IsAny<string[]>(), It.IsAny<string[]>())).Returns(new string[1] { $"test.txt" });

        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        _blobStorageFileUploaderHandlerMock.Verify(mf => mf.UploadFromStreamAsync(It.IsAny<Uri>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_NoFilesToUpload_ReturnStatusFailed()
    {

        _fileStreamerWrapperMock.Setup(x => x.Concat(It.IsAny<string[]>(), It.IsAny<string[]>())).Returns(new string[] { });

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }
    [Test]
    public async Task UploadFilesToBlobStorageAsync_EmptyDirectory_ReturnStatusFailed()
    {

        _fileStreamerWrapperMock.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(new string[] { });
        _fileStreamerWrapperMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }
}