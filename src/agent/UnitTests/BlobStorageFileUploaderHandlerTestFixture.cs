using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Entities;
using Microsoft.Azure.Storage.Blob;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Storage.Core.Util;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Azure.Devices.Client.Transport;

[TestFixture]
public class BlobStorageFileUploaderHandlerTestFixture
{
    private Mock<ICloudBlockBlobWrapper> _cloudBlockBlobWrapperMock;
    private Mock<ITwinReportHandler> _twinReportHandlerMock;
    private Mock<ILoggerHandler> _loggerMock;
    private IBlobStorageFileUploaderHandler _target;
    private FileUploadCompletionNotification notification = new FileUploadCompletionNotification();


    [SetUp]
    public void Setup()
    {
        _cloudBlockBlobWrapperMock = new Mock<ICloudBlockBlobWrapper>();
        _twinReportHandlerMock = new Mock<ITwinReportHandler>();
        _loggerMock = new Mock<ILoggerHandler>();
        _target = new BlobStorageFileUploaderHandler(_cloudBlockBlobWrapperMock.Object, _twinReportHandlerMock.Object, _loggerMock.Object);
    }

    [Test]
    public async Task UploadFromStreamAsync_ValidInput_UploadsStreamToBlob()
    {
        var storageUri = new Uri("https://mockstorage.example.com/mock-container");
        var readStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cancellationToken = CancellationToken.None;
        var _mockBlockBlob = new Mock<CloudBlockBlob>(storageUri);

        _cloudBlockBlobWrapperMock
        .Setup(b => b.CreateCloudBlockBlob(It.IsAny<Uri>()))
        .Returns(_mockBlockBlob.Object);

        _cloudBlockBlobWrapperMock
            .Setup(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>(), It.IsAny<IProgress<StorageProgress>>(), cancellationToken))
            .Returns(Task.CompletedTask);
        var actionToReport = new ActionToReport();
        await _target.UploadFromStreamAsync(notification, storageUri, readStream, actionToReport, cancellationToken);

        // Verify that UploadFromStreamAsync was called with the provided stream and cancellation token
        _cloudBlockBlobWrapperMock.Verify(
        b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>(), It.IsAny<IProgress<StorageProgress>>(), CancellationToken.None),
        Times.Once);



    }

    [Test]
    public async Task UploadFromStreamAsync_InvalidInput_DoesNotUploadStream()
    {
        Uri invalidStorageUri = null; // Or provide an invalid URI here
        var readStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cancellationToken = CancellationToken.None;

        Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                // Call the method that should throw the exception
                await _target.UploadFromStreamAsync(notification, invalidStorageUri, readStream, new ActionToReport(), cancellationToken);
            });
    }
}
