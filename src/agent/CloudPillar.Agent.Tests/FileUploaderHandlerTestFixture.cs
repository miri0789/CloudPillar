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
    FileUploadSasUriResponse sasUriResponse = new FileUploadSasUriResponse()
    {
        BlobName = "nechama-device/C_driveroot_/git.dev/CloudPillar/UploadFolder/uploadFileInFolder.txt",
        ContainerName = "nechama-container",
        CorrelationId = "MjAyMzA5MDcwOTI5XzNlZjBmOWM1LTQ3NTEtNDM4OC1hMWJjLWJlZGVmZmI0ZTdiM19DX2RyaXZlcm9vdF8vZ2l0LmRldi9DbG91ZFBpbGxhci9VcGxvYWRGb2xkZXIvdXBsb2FkRmlsZUluRm9sZGVyLnR4dF92ZXIyLjA=",
        HostName = "nechama.blob.core.windows.net",
        SasToken = "?sv=2018-03-28&sr=b&sig=wwdV5O%2FY2O3TVaAHn2jHZ17fQeKSW0rgkr1jcSxcTwE%3D&se=2023-09-07T09%3A19%3A58Z&sp=rw"
    };

    [SetUp]
    public void Setup()
    {
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _blobStorageFileUploaderHandlerMock = new Mock<IBlobStorageFileUploaderHandler>();
        _streamingFileUploaderHandlerMock = new Mock<IStreamingFileUploaderHandler>();
        _twinActionsHandler = new Mock<ITwinActionsHandler>();
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
            FileName = "C:\\demo"
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
            FileName = "C:\\demo"
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
            FileName = "C:\\demo\\stream2.jpg"
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
            FileName = "C:\\git.dev\\CloudPillar\\no-exists.txt"
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
            FileName = "C:\\git.dev\\CloudPillar\\no-exists-folder"
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
            FileName = "C:\\git.dev\\CloudPillar\\EmptyFolder"
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
