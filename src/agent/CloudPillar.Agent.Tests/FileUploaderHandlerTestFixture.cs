using Moq;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;
using CloudPillar.Agent.Entities;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.WindowsAzure.Storage.Blob;
using Shared.Logger;

[TestFixture]
public class FileUploaderHandlerTestFixture
{
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<IBlobStorageFileUploaderHandler> _blobStorageFileUploaderHandlerMock;
    private Mock<IStreamingFileUploaderHandler> _streamingFileUploaderHandlerMock;
    private Mock<ITwinActionsHandler> _twinActionsHandler;
    private Mock<ILoggerHandler> _loggerMock;
    private IFileUploaderHandler _target;

    const string BAES_PATH = "c:\\demo";
    private Uri STORAGE_URI = new Uri("https://mockstorage.example.com/mock-container");

    FileUploadSasUriResponse sasUriResponse = new FileUploadSasUriResponse();

    [SetUp]
    public void Setup()
    {
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _blobStorageFileUploaderHandlerMock = new Mock<IBlobStorageFileUploaderHandler>();
        _streamingFileUploaderHandlerMock = new Mock<IStreamingFileUploaderHandler>();
        _loggerMock = new Mock<ILoggerHandler>();

        _twinActionsHandler = new Mock<ITwinActionsHandler>();
        _deviceClientWrapperMock.Setup(device => device.GetFileUploadSasUriAsync(It.IsAny<FileUploadSasUriRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sasUriResponse);

        _deviceClientWrapperMock.Setup(device => device.GetBlobUriAsync(It.IsAny<FileUploadSasUriResponse>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(STORAGE_URI);
        _loggerMock = new Mock<ILoggerHandler>();
        _deviceClientWrapperMock.Setup(device => device.GetFileUploadSasUriAsync(It.IsAny<FileUploadSasUriRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sasUriResponse);

        _target = new FileUploaderHandler(_deviceClientWrapperMock.Object, _blobStorageFileUploaderHandlerMock.Object, _streamingFileUploaderHandlerMock.Object,
        _twinActionsHandler.Object, _loggerMock.Object);
    }


    [Test]
    public async Task UploadFilesToBlobStorageAsync_ValidStorageFiles_ReturnStatusSuccess()
    {

        var uploadAction = new UploadAction
        {
            FileName = BAES_PATH
        };

        var actionToReport = new ActionToReport
        {
            TwinReport = new TwinActionReported()
        };
        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        _blobStorageFileUploaderHandlerMock.Verify(mf => mf.UploadFromStreamAsync(It.IsAny<Uri>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_ValidPathDirectory_ReturnStatusSuccess()
    {
        var uploadAction = new UploadAction
        {
            FileName = BAES_PATH
        };

        var actionToReport = new ActionToReport
        {
            TwinReport = new TwinActionReported()
        };

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Success);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_ValidPathFile_ReturnStatusSuccess()
    {
        var uploadAction = new UploadAction
        {
            FileName = $"{BAES_PATH}\\test.txt"
        };

        var actionToReport = new ActionToReport
        {
            TwinReport = new TwinActionReported()
        };

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Success);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_NoExistsFile_ReturnStatusFailed()
    {
        var uploadAction = new UploadAction
        {
            FileName = $"{BAES_PATH}\\no-exists.txt"
        };

        var actionToReport = new ActionToReport
        {
            TwinReport = new TwinActionReported()
        };

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_NoExistsFolder_ReturnStatusFailed()
    {
        var uploadAction = new UploadAction
        {
            FileName = $"{BAES_PATH}\\no-exists-folder"
        };

        var actionToReport = new ActionToReport
        {
            TwinReport = new TwinActionReported()
        };

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_NoFilesToUpload_ReturnStatusFailed()
    {
        var uploadAction = new UploadAction
        {
            FileName = $"{BAES_PATH}\\EmptyFolder"
        };

        var actionToReport = new ActionToReport
        {
            TwinReport = new TwinActionReported()
        };

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_EmptyPath_ReturnStatusFailed()
    {

        var uploadAction = new UploadAction
        {
            FileName = ""
        };

        var actionToReport = new ActionToReport
        {
            TwinReport = new TwinActionReported()
        };

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }
}
