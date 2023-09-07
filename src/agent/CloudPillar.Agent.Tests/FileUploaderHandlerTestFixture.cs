using Moq;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;
using CloudPillar.Agent.Entities;
using Microsoft.Azure.Devices.Client.Transport;

[TestFixture]
public class FileUploaderHandlerTestFixture
{
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<IBlobStorageFileUploaderHandler> _blobStorageFileUploaderHandlerMock;
    private Mock<IIoTStreamingFileUploaderHandler> _ioTStreamingFileUploaderHandlerMock;
    private IFileUploaderHandler _target;

    CancellationToken cancellationToken = CancellationToken.None;
    UploadAction uploadAction;
    ActionToReport actionToReport;
    FileUploadSasUriResponse sasUriResponseForFile = new FileUploadSasUriResponse()
    {
        BlobName = "nechama-device/C_driveroot_/git.dev/CloudPillar/UploadFolder/uploadFileInFolder.txt",
        ContainerName = "nechama-container",
        CorrelationId = "MjAyMzA5MDcwOTI5XzNlZjBmOWM1LTQ3NTEtNDM4OC1hMWJjLWJlZGVmZmI0ZTdiM19DX2RyaXZlcm9vdF8vZ2l0LmRldi9DbG91ZFBpbGxhci9VcGxvYWRGb2xkZXIvdXBsb2FkRmlsZUluRm9sZGVyLnR4dF92ZXIyLjA=",
        HostName = "nechama.blob.core.windows.net",
        SasToken = "?sv=2018-03-28&sr=b&sig=wwdV5O%2FY2O3TVaAHn2jHZ17fQeKSW0rgkr1jcSxcTwE%3D&se=2023-09-07T09%3A19%3A58Z&sp=rw"
    };

    FileUploadSasUriResponse sasUriResponseForDirectory = new FileUploadSasUriResponse()
    {
        BlobName = "nechama-device/c_driveroot_/upload.zip",
        ContainerName = "nechama-container",
        CorrelationId = "MjAyMzA5MDcwNzI0XzZiNDExM2ZlLTQ1MzktNDg2YS05YmRkLWIyY2QzMmUwM2U0NF9jX2RyaXZlcm9vdF8vdXBsb2FkLnppcF92ZXIyLjA=",
        HostName = "nechama.blob.core.windows.net",
        SasToken = "?sv=2018-03-28&sr=b&sig=itXnKMeEUEK5PuSwGgK0Gc4llSUoS3BndM1yiNo6GeE%3D&se=2023-09-07T07%3A14%3A15Z&sp=rw"
    };

    [SetUp]
    public void Setup()
    {
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _blobStorageFileUploaderHandlerMock = new Mock<IBlobStorageFileUploaderHandler>();
        _ioTStreamingFileUploaderHandlerMock = new Mock<IIoTStreamingFileUploaderHandler>();

        _target = new FileUploaderHandler(_deviceClientWrapperMock.Object, _blobStorageFileUploaderHandlerMock.Object, _ioTStreamingFileUploaderHandlerMock.Object);
    }

    private void InitFileUpload(string fileName, bool enabled = true)
    {
        uploadAction = new UploadAction
        {
            FileName = fileName,
            Enabled = enabled,
        };

        actionToReport = new ActionToReport
        {
            TwinReport = new TwinActionReported()
        };
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_ExecInnerUploadFunction_ReturnStatusSuccess()
    {

        InitFileUpload("C:\\upload");

        _deviceClientWrapperMock.Setup(device => device.GetFileUploadSasUriAsync(It.IsAny<FileUploadSasUriRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(sasUriResponseForDirectory);

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, cancellationToken);

        _blobStorageFileUploaderHandlerMock.Verify(mf => mf.UploadFromStreamAsync(It.IsAny<Uri>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_ValidPathDirectory_ReturnStatusSuccess()
    {

        InitFileUpload("C:\\git.dev\\CloudPillar\\UploadFolder");

        _deviceClientWrapperMock.Setup(device => device.GetFileUploadSasUriAsync(It.IsAny<FileUploadSasUriRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(sasUriResponseForDirectory);

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, cancellationToken);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Success);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_ValidPathFile_ReturnStatusSuccess()
    {

        InitFileUpload("C:\\git.dev\\CloudPillar\\uploadFile.txt");

        _deviceClientWrapperMock.Setup(device => device.GetFileUploadSasUriAsync(It.IsAny<FileUploadSasUriRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(sasUriResponseForFile);

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, cancellationToken);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Success);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_NoExistsFile_ReturnStatusFailed()
    {

        InitFileUpload("C:\\git.dev\\CloudPillar\\no-exists.txt");

        _deviceClientWrapperMock.Setup(device => device.GetFileUploadSasUriAsync(It.IsAny<FileUploadSasUriRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(sasUriResponseForFile);

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, cancellationToken);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_NoExistsFolder_ReturnStatusFailed()
    {
        InitFileUpload("C:\\git.dev1\\CloudPillar\\no-exists-folder");

        _deviceClientWrapperMock.Setup(device => device.GetFileUploadSasUriAsync(It.IsAny<FileUploadSasUriRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(sasUriResponseForDirectory);

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, cancellationToken);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_NoFilesToUpload_ReturnStatusFailed()
    {
        InitFileUpload("C:\\git.dev\\CloudPillar\\EmptyFolder");

        _deviceClientWrapperMock.Setup(device => device.GetFileUploadSasUriAsync(It.IsAny<FileUploadSasUriRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(sasUriResponseForDirectory);

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, cancellationToken);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_EmptyPath_ReturnStatusFailed()
    {

        InitFileUpload("");

        _deviceClientWrapperMock.Setup(device => device.GetFileUploadSasUriAsync(It.IsAny<FileUploadSasUriRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(sasUriResponseForDirectory);

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, cancellationToken);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }
}
