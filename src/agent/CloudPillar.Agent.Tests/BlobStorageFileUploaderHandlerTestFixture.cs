using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Entities;
using Microsoft.WindowsAzure.Storage.Blob;
using CloudPillar.Agent.Wrappers;

[TestFixture]
public class BlobStorageFileUploaderHandlerTestFixture
{
    private Mock<ICloudBlockBlobWrapper> _cloudBlockBlobWrapperMock;
    private IBlobStorageFileUploaderHandler _target;

    [SetUp]
    public void Setup()
    {
        _cloudBlockBlobWrapperMock = new Mock<ICloudBlockBlobWrapper>();
        _target = new BlobStorageFileUploaderHandler(_cloudBlockBlobWrapperMock.Object);
    }

    [Test]
    public async Task UploadFromStreamAsync_ValidInput_UploadsStreamToBlob()
    {
        var storageUri = new Uri("https://nechama.blob.core.windows.net/nechama-container");
        var readStream = new MemoryStream(new byte[] { 1, 2, 3 });
        var cancellationToken = CancellationToken.None;
        var _mockBlockBlob = new Mock<CloudBlockBlob>(storageUri);

        _cloudBlockBlobWrapperMock
        .Setup(b => b.CreateCloudBlockBlob(It.IsAny<Uri>()))
        .Returns(_mockBlockBlob.Object);

        _cloudBlockBlobWrapperMock
            .Setup(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>(), cancellationToken))
            .Returns(Task.CompletedTask);

        await _target.UploadFromStreamAsync(storageUri, readStream, cancellationToken);

        // Verify that UploadFromStreamAsync was called with the provided stream and cancellation token
        _cloudBlockBlobWrapperMock.Verify(
        b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>(), CancellationToken.None),
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
                await _target.UploadFromStreamAsync(invalidStorageUri, readStream, cancellationToken);
            });
    }
}
