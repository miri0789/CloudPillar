using Backend.BlobStreamer.Services;
using Microsoft.Azure.Storage.Blob;
using Moq;
using Shared.Logger;
using Shared.Entities.Services;
using Microsoft.Extensions.Options;
using Backend.BlobStreamer.Services.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Backend.BlobStreamer.Wrappers.Interfaces;

namespace Backend.BlobStreamer.Tests
{

    [TestFixture]
    public class UploadStreamChunksServiceTestFixture
    {
        private Mock<ICheckSumService> _mockCheckSumService;
        private Mock<ICloudBlockBlobWrapper> _mockCloudBlockBlobWrapper;
        private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
        private Mock<ITwinDiseredService> _mockTwinDiseredService;
        private Mock<ILoggerHandler> _mockLogger;
        private IUploadStreamChunksService _target;
        private readonly Uri STORAGE_URI = new Uri("https://mockstorage.example.com/mock-container");
        private byte[] BYTES = { 1, 2, 3 };
        private const long _startPosition = 0;
        private const string _checkSum = "xxx";

        [SetUp]
        public void Setup()
        {
            _mockCheckSumService = new Mock<ICheckSumService>();
            _mockCloudBlockBlobWrapper = new Mock<ICloudBlockBlobWrapper>();
            _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
            _mockLogger = new Mock<ILoggerHandler>();
            _mockTwinDiseredService = new Mock<ITwinDiseredService>();

            createMockBlob(STORAGE_URI);
            _mockCloudBlockBlobWrapper.Setup(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>())).Returns(Task.CompletedTask);

            _target = new UploadStreamChunksService(_mockLogger.Object, _mockCheckSumService.Object, _mockCloudBlockBlobWrapper.Object, _mockTwinDiseredService.Object,  _mockEnvironmentsWrapper.Object);
        }

        private void createMockBlob(Uri storageUri)
        {
            var _mockBlockBlob = new Mock<CloudBlockBlob>(storageUri);
            _mockCloudBlockBlobWrapper.Setup(b => b.CreateCloudBlockBlob(It.IsAny<Uri>())).Returns(_mockBlockBlob.Object);
        }

        [Test]
        public async Task UploadStreamChunkAsync_ValidData_Success()
        {

            await _target.UploadStreamChunkAsync(STORAGE_URI, BYTES, 0, "", "", false, "");
            _mockCloudBlockBlobWrapper.Verify(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>()), Times.Once);
        }

        [Test]
        public async Task UploadStreamChunkAsync_EmptyStorageUri_NotUpload()
        {
            Uri? emptyUri = null;

            await _target.UploadStreamChunkAsync(emptyUri, BYTES, _startPosition, _checkSum, "", false, "");
            _mockCloudBlockBlobWrapper.Verify(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>()), Times.Never);
        }

        [Test]
        public async Task UploadStreamChunkAsync_BlobExists_DownloadExistsData()
        {

            _mockCloudBlockBlobWrapper.Setup(b => b.BlobExists(It.IsAny<CloudBlockBlob>())).ReturnsAsync(true);

            await _target.UploadStreamChunkAsync(STORAGE_URI, BYTES, 0, "", "", false, "");
            _mockCloudBlockBlobWrapper.Verify(b => b.DownloadToStreamAsync(It.IsAny<CloudBlockBlob>()), Times.Once);
        }
    }
}
