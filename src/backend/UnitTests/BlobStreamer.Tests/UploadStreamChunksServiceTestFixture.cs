using Backend.BlobStreamer.Services;
using Microsoft.Azure.Storage.Blob;
using Moq;
using Shared.Logger;
using Shared.Entities.Services;
using Backend.BlobStreamer.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Backend.BlobStreamer.Services.Interfaces;
using Shared.Entities.Twin;
using Shared.Entities.Messages;

namespace Backend.BlobStreamer.Tests
{

    [TestFixture]
    public class UploadStreamChunksServiceTestFixture
    {
        private Mock<ICheckSumService> _mockCheckSumService;
        private Mock<ICloudBlockBlobWrapper> _mockCloudBlockBlobWrapper;
        private Mock<ITwinDiseredService> _mockTwinDiseredService;
        private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
        private Mock<ICloudStorageWrapper> _mockCloudStorageWrapper;
        private Mock<IChangeSpecService> _mockChangeSpecService;
        private Mock<IBlobService> _mockBlobService;
        private Mock<ILoggerHandler> _mockLogger;
        private Mock<CloudBlockBlob> _mockCloudBlockBlob;
        private IUploadStreamChunksService _target;
        private readonly Uri STORAGE_URI = new Uri("https://mockstorage.example.com/mockcontainer/n-12%2FC_driveroot_%2FUsers%2FTest%2FAppData%2FLocal%2FTemp%2Ftest.tmp");
        private byte[] BYTES = { 1, 2, 3 };
        private const long _startPosition = 0;
        private const string _checkSum = "xxx";

        [SetUp]
        public void Setup()
        {
            _mockCheckSumService = new Mock<ICheckSumService>();
            _mockCloudBlockBlobWrapper = new Mock<ICloudBlockBlobWrapper>();
            _mockLogger = new Mock<ILoggerHandler>();
            _mockTwinDiseredService = new Mock<ITwinDiseredService>();
            _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
            _mockCloudStorageWrapper = new Mock<ICloudStorageWrapper>();
            _mockChangeSpecService = new Mock<IChangeSpecService>();
            _mockBlobService = new Mock<IBlobService>();
            _mockCloudBlockBlob = new Mock<CloudBlockBlob>();

            createMockBlob(STORAGE_URI);
            _mockCloudBlockBlobWrapper.Setup(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>())).Returns(Task.CompletedTask);

            _target = new UploadStreamChunksService(_mockLogger.Object, _mockCheckSumService.Object, _mockCloudBlockBlobWrapper.Object,
             _mockCloudStorageWrapper.Object, _mockTwinDiseredService.Object, _mockEnvironmentsWrapper.Object,
              _mockChangeSpecService.Object, _mockBlobService.Object);
        }

        private void createMockBlob(Uri storageUri)
        {
            _mockCloudBlockBlob = new Mock<CloudBlockBlob>(storageUri);
            _mockCloudBlockBlobWrapper.Setup(b => b.CreateCloudBlockBlob(It.IsAny<Uri>())).Returns(_mockCloudBlockBlob.Object);
        }

        [Test]
        public async Task UploadStreamChunkAsync_ValidData_Success()
        {

            await _target.UploadStreamChunkAsync(STORAGE_URI, BYTES, 0, "", "", "", false);
            _mockCloudBlockBlobWrapper.Verify(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>()), Times.Once);
        }

        [Test]
        public async Task UploadStreamChunkAsync_EmptyStorageUri_NotUpload()
        {
            Uri? emptyUri = null;

            await _target.UploadStreamChunkAsync(emptyUri, BYTES, _startPosition, _checkSum, "", "", false);
            _mockCloudBlockBlobWrapper.Verify(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>()), Times.Never);
        }

        [Test]
        public async Task UploadStreamChunkAsync_BlobExists_DownloadExistsData()
        {

            _mockCloudBlockBlobWrapper.Setup(b => b.BlobExists(It.IsAny<CloudBlockBlob>())).ReturnsAsync(true);

            await _target.UploadStreamChunkAsync(STORAGE_URI, BYTES, 0, "", "", "", false);
            _mockCloudBlockBlobWrapper.Verify(b => b.DownloadToStreamAsync(It.IsAny<CloudBlockBlob>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadForDiagnosticsAsync_DestinationPath_BuildDestionationBySource()
        {
            await _target.HandleDownloadForDiagnosticsAsync("", STORAGE_URI, _mockCloudBlockBlob.Object);

            var destionationPath = "C:\\Users\\Test\\AppData\\Local\\Temp\\test.tmp";

            _mockTwinDiseredService.Verify(b => b.AddDesiredRecipeAsync(It.IsAny<string>(), SharedConstants.CHANGE_SPEC_DIAGNOSTICS_NAME,
            It.Is<DownloadAction>(x => x.DestinationPath == destionationPath), It.IsAny<int>(), SharedConstants.DEFAULT_TRANSACTIONS_KEY), Times.Once);
        }

        [Test]
        public async Task HandleDownloadForDiagnosticsAsync_SignCertificateFile_Success()
        {
            await _target.HandleDownloadForDiagnosticsAsync("", STORAGE_URI, _mockCloudBlockBlob.Object);
            _mockChangeSpecService.Verify(x => x.SendToSignData(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadForDiagnosticsAsync_CalculateHashAsync_Success()
        {
            await _target.HandleDownloadForDiagnosticsAsync("", STORAGE_URI, _mockCloudBlockBlob.Object);
            _mockBlobService.Verify(x => x.CalculateHashAsync(It.IsAny<string>(), It.Is<SignFileEvent>(c => c.BufferSize == SharedConstants.SIGN_FILE_BUFFER_SIZE && c.FileName == "test.tmp"), It.IsAny<CloudBlockBlob>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadForDiagnosticsAsync_UpdateChangeSpecSign_Success()
        {
            await _target.HandleDownloadForDiagnosticsAsync("", STORAGE_URI, _mockCloudBlockBlob.Object);
            _mockChangeSpecService.Verify(x => x.CreateChangeSpecKeySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TwinDesired>()), Times.Once);
        }
    }
}
