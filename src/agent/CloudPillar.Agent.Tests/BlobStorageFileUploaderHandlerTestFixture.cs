using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Entities;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.Devices.Client.Transport;
using CloudPillar.Agent.Wrappers;

[TestFixture]
public class BlobStorageFileUploaderHandlerTestFixture
{
    private Mock<ICloudBlockBlobWrapper> _cloudBlockBlobWrapperMock;
    private IBlobStorageFileUploaderHandler _target;

    FileUploadSasUriResponse fileUploadSasUriResponse = new FileUploadSasUriResponse()
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
        _cloudBlockBlobWrapperMock = new Mock<ICloudBlockBlobWrapper>();
        _target = new BlobStorageFileUploaderHandler(_cloudBlockBlobWrapperMock.Object);
    }

    [Test]
    public async Task UploadFromStreamAsync_ValidInput_UploadsStreamToBlob()
    {
        Uri storageUri = fileUploadSasUriResponse.GetBlobUri();
        MemoryStream readStream = new MemoryStream(new byte[] { 1, 2, 3 });
        CancellationToken cancellationToken = CancellationToken.None;
        CloudBlockBlob cloudBlockBlob = new CloudBlockBlob(storageUri);

        _cloudBlockBlobWrapperMock
        .Setup(b => b.CreateCloudBlockBlob(It.IsAny<Uri>()))
        .Returns(cloudBlockBlob);

        _cloudBlockBlobWrapperMock
            .Setup(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>(), cancellationToken))
            .Returns(Task.CompletedTask);
 
         await _target.UploadFromStreamAsync(storageUri, readStream, cancellationToken);

        // Verify that UploadFromStreamAsync was called with the provided stream and cancellation token
        _cloudBlockBlobWrapperMock.Verify(
            b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
