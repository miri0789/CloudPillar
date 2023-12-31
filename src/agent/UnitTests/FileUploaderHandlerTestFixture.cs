using Moq;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;
using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Azure.Devices.Client.Transport;

[TestFixture]
public class FileUploaderHandlerTestFixture
{
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<IBlobStorageFileUploaderHandler> _blobStorageFileUploaderHandlerMock;
    private Mock<IStreamingFileUploaderHandler> _streamingFileUploaderHandlerMock;
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private Mock<IStrictModeHandler> _strictModeHandlerMock;
    private Mock<ITwinReportHandler> _twinReportHandler;
    private Mock<ILoggerHandler> _loggerMock;
    private IFileUploaderHandler _target;
    private const string FILE_NAME = "testFileName";
    private const string CHANGE_SPEC_ID = "123.456.789";
    private UploadAction uploadAction = new UploadAction
    {
        FileName = FILE_NAME,
        Method = FileUploadMethod.Blob
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
        _twinReportHandler = new Mock<ITwinReportHandler>();
        _strictModeHandlerMock = new Mock<IStrictModeHandler>();
        _loggerMock = new Mock<ILoggerHandler>();

        _deviceClientWrapperMock.Setup(device => device.GetFileUploadSasUriAsync(It.IsAny<FileUploadSasUriRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new FileUploadSasUriResponse());
        var storageUri = new Uri("https://mockstorage.example.com/mock-container");
        _deviceClientWrapperMock.Setup(device => device.GetBlobUri(It.IsAny<FileUploadSasUriResponse>()))
        .Returns(storageUri);

        _fileStreamerWrapperMock.Setup(f => f.CreateStream(It.IsAny<string>(), It.IsAny<FileMode>(), It.IsAny<FileAccess>(), It.IsAny<FileShare>(), It.IsAny<int>(), It.IsAny<bool>()))
        .Returns(() => new MemoryStream(new byte[] { 1, 2, 3 }));
        _fileStreamerWrapperMock.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new string[] { });
        _fileStreamerWrapperMock.Setup(x => x.GetDirectories(It.IsAny<string>(), It.IsAny<string>())).Returns(new string[] { });

        _target = new FileUploaderHandler(_deviceClientWrapperMock.Object, _fileStreamerWrapperMock.Object, _blobStorageFileUploaderHandlerMock.Object, _streamingFileUploaderHandlerMock.Object,
        _twinReportHandler.Object, _loggerMock.Object, _strictModeHandlerMock.Object);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_ValidStorageFiles_UploadStream()
    {
        _fileStreamerWrapperMock.Setup(x => x.Concat(It.IsAny<string[]>(), It.IsAny<string[]>())).Returns(new string[1] { $"test.txt" });

        await _target.FileUploadAsync(actionToReport, uploadAction.Method, uploadAction.FileName, CHANGE_SPEC_ID, CancellationToken.None);

        _blobStorageFileUploaderHandlerMock.Verify(mf => mf.UploadFromStreamAsync(It.IsAny<FileUploadCompletionNotification>(), It.IsAny<Uri>(), It.IsAny<Stream>(), It.IsAny<ActionToReport>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_InvalidFileAccessPermissions_ReturnStatusFailed()
    {
        _fileStreamerWrapperMock.Setup(x => x.Concat(It.IsAny<string[]>(), It.IsAny<string[]>())).Returns(new string[1] { $"test.txt" });
        _strictModeHandlerMock.Setup(x => x.CheckFileAccessPermissions(TwinActionType.SingularUpload, It.IsAny<string>())).Throws(new Exception());

        await _target.FileUploadAsync(actionToReport, uploadAction.Method, uploadAction.FileName, CHANGE_SPEC_ID, CancellationToken.None);

        Assert.That(actionToReport.TwinReport.Status, Is.EqualTo(StatusType.Failed));
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_ValidChangeSpecId_CreateBlobUrlWithId()
    {
        _fileStreamerWrapperMock.Setup(x => x.Concat(It.IsAny<string[]>(), It.IsAny<string[]>())).Returns(new string[1] { $"test.txt" });

        await _target.FileUploadAsync(actionToReport, uploadAction.Method, uploadAction.FileName, CHANGE_SPEC_ID, CancellationToken.None);

        _deviceClientWrapperMock.Verify(mf => mf.GetFileUploadSasUriAsync(It.Is<FileUploadSasUriRequest>(file => file.BlobName.Contains(CHANGE_SPEC_ID)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_NoFilesToUpload_ReturnStatusFailed()
    {

        _fileStreamerWrapperMock.Setup(x => x.Concat(It.IsAny<string[]>(), It.IsAny<string[]>())).Returns(new string[] { });

        // Act
        await _target.FileUploadAsync(actionToReport, uploadAction.Method, uploadAction.FileName, CHANGE_SPEC_ID, CancellationToken.None);

        Assert.That(actionToReport.TwinReport.Status, Is.EqualTo(StatusType.Failed));
    }
    [Test]
    public async Task UploadFilesToBlobStorageAsync_EmptyDirectory_ReturnStatusFailed()
    {

        _fileStreamerWrapperMock.Setup(x => x.GetFiles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SearchOption>())).Returns(new string[] { });
        _fileStreamerWrapperMock.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
        // Act
        await _target.FileUploadAsync(actionToReport, uploadAction.Method, uploadAction.FileName, CHANGE_SPEC_ID, CancellationToken.None);

        Assert.That(actionToReport.TwinReport.Status, Is.EqualTo(StatusType.Failed));
    }


}