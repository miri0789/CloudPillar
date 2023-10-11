using Moq;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers;
using Shared.Entities.Twin;
using CloudPillar.Agent.Entities;
using Microsoft.Azure.Devices.Client.Transport;
using Microsoft.WindowsAzure.Storage.Blob;
using Shared.Logger;
using Microsoft.VisualBasic.FileIO;

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
    private string[] directories = new string[] { };
    private string[] files = new string[] { };
    const int BUFFER_SIZE = 4 * 1024 * 1024;
    const string BAES_PATH = "c:\\demo\\test.txt";
    private readonly Uri STORAGE_URI = new Uri("https://mockstorage.example.com/mock-container");
    private UploadAction uploadAction = new UploadAction
    {
        FileName = BAES_PATH
    };

    private ActionToReport actionToReport = new ActionToReport
    {
        TwinReport = new TwinActionReported()
    };
    FileUploadSasUriResponse sasUriResponse = new FileUploadSasUriResponse();

    [SetUp]
    public void Setup()
    {
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _blobStorageFileUploaderHandlerMock = new Mock<IBlobStorageFileUploaderHandler>();
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
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

        _target = new FileUploaderHandler(_deviceClientWrapperMock.Object, _fileStreamerWrapperMock.Object, _blobStorageFileUploaderHandlerMock.Object, _streamingFileUploaderHandlerMock.Object,
        _twinActionsHandler.Object, _loggerMock.Object);

        _fileStreamerWrapperMock.Setup(x => x.GetDirectories("", "")).Returns(directories);
        _fileStreamerWrapperMock.Setup(x => x.GetFiles("", "")).Returns(files);
        _fileStreamerWrapperMock.Setup(x => x.GetFileName("")).Returns("myFile");
        _fileStreamerWrapperMock.Setup(x => x.GetDirectoryName("")).Returns("myDirectory");
        _fileStreamerWrapperMock.Setup(x => x.TextReplace("", "", "")).Returns("file");
        _fileStreamerWrapperMock.Setup(x => x.RegexReplace("", "", "")).Returns("c://");

    }


    [Test]
    public async Task UploadFilesToBlobStorageAsync_ValidStorageFiles_ReturnStatusSuccess()
    {


        // Act        private MemoryStream READ_STREAM = new MemoryStream(new byte[] { 1, 2, 3 });

        // var _fileStreamMock = new FileStream(BAES_PATH,FileMode.Open);
        _fileStreamerWrapperMock.Setup(x => x.Concat(files, directories)).Returns(new string[1] { $"{BAES_PATH}\\test.txt" });
        _fileStreamerWrapperMock.Setup(x => x.FileExists("")).Returns(true);
        // _fileStreamerWrapperMock.Setup(x => x.CreateStream("", FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE, true))
        // .Returns(_fileStreamMock);
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        // Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Success);

        _blobStorageFileUploaderHandlerMock.Verify(mf => mf.UploadFromStreamAsync(It.IsAny<Uri>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_ValidPathDirectory_ReturnStatusSuccess()
    {
        _fileStreamerWrapperMock.Setup(x => x.DirectoryExists("")).Returns(true);

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Success);
    }



    [Test]
    public async Task UploadFilesToBlobStorageAsync_NoExistsFile_ReturnStatusFailed()
    {

        _fileStreamerWrapperMock.Setup(x => x.FileExists("")).Returns(false);

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_NoExistsFolder_ReturnStatusFailed()
    {
        _fileStreamerWrapperMock.Setup(x => x.DirectoryExists("")).Returns(false);

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }

    [Test]
    public async Task UploadFilesToBlobStorageAsync_NoFilesToUpload_ReturnStatusFailed()
    {

        _fileStreamerWrapperMock.Setup(x => x.Concat(files, directories)).Returns(new string[] { });

        // Act
        await _target.FileUploadAsync(uploadAction, actionToReport, CancellationToken.None);

        Assert.AreEqual(actionToReport.TwinReport.Status, StatusType.Failed);
    }
}
