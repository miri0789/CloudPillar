using Backend.BlobStreamer.Services;
using Microsoft.Azure.Storage.Blob;
using Moq;
using Shared.Logger;
using Shared.Entities.Services;
using Backend.BlobStreamer.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Backend.BlobStreamer.Services.Interfaces;
using Shared.Entities.Twin;
using Shared.Enums;

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
        private Mock<ILoggerHandler> _mockLogger;
        private IUploadStreamChunksService _target;
        private readonly Uri STORAGE_URI = new Uri("https://mockstorage.example.com/mockcontainer/n-12%2FC_driveroot_%2FUsers%2FTest%2FAppData%2FLocal%2FTemp%2Ftest.tmp");
        private readonly Uri STORAGE_URI_SETTINGS = new Uri("https://mockstorage.example.com/mockcontainer/n-100Settings%2FC_driveroot_%2FUsers%2FTest%2FAppData%2FLocal%2FTemp%2Ftestettings.tmp");
        private byte[] BYTES = { 1, 2, 3 };
        private const long _startPosition = 0;
        private const string _checkSum = "xxx";
        private CloudBlobContainer _container;

        [SetUp]
        public void Setup()
        {
            _mockCheckSumService = new Mock<ICheckSumService>();
            _mockCloudBlockBlobWrapper = new Mock<ICloudBlockBlobWrapper>();
            _mockLogger = new Mock<ILoggerHandler>();
            _mockTwinDiseredService = new Mock<ITwinDiseredService>();
            _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
            _mockCloudStorageWrapper = new Mock<ICloudStorageWrapper>();

            _container = new CloudBlobContainer(STORAGE_URI_SETTINGS);
            _mockCloudStorageWrapper.Setup(c => c.GetBlobContainer(It.IsAny<string>(), It.IsAny<string>())).Returns(_container);
            _mockEnvironmentsWrapper.Setup(e => e.storageConnectionString).Returns("DefaultEndpointsProtocol=https;AccountName=mockstorage;AccountKey=abc;EndpointSuffix=core.windows.net");
            _mockEnvironmentsWrapper.Setup(e => e.blobContainerName).Returns("iotcontainer");

            _target = new UploadStreamChunksService(_mockLogger.Object, _mockCheckSumService.Object, _mockCloudBlockBlobWrapper.Object, _mockCloudStorageWrapper.Object, _mockTwinDiseredService.Object, _mockEnvironmentsWrapper.Object);
            // _container = new CloudBlobContainer(STORAGE_URI_SETTINGS);
            createMockBlob(STORAGE_URI);
            _mockCloudBlockBlobWrapper.Setup(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>())).Returns(Task.CompletedTask);

            _target = new UploadStreamChunksService(_mockLogger.Object, _mockCheckSumService.Object, _mockCloudBlockBlobWrapper.Object, _mockCloudStorageWrapper.Object, _mockTwinDiseredService.Object, _mockEnvironmentsWrapper.Object);
        }

        private void createMockBlob(Uri storageUri)
        {
            var _mockBlockBlob = new Mock<CloudBlockBlob>(storageUri);
            _mockCloudBlockBlobWrapper.Setup(b => b.CreateCloudBlockBlob(It.IsAny<Uri>())).Returns(_mockBlockBlob.Object);
        }

        [Test]
        public async Task UploadStreamChunkAsync_ValidData_Success()
        {

            await _target.UploadStreamChunkAsync(STORAGE_URI, BYTES, 0, "", "", "", false);
            _mockCloudBlockBlobWrapper.Verify(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>()), Times.Once);
        }

        [Test]
        public async Task UploadStreamChunkAsync_EmptyStorageUri_ContinueWithEnviromentDetails()
        {
            Uri? emptyUri = null;

            await _target.UploadStreamChunkAsync(emptyUri, BYTES, _startPosition, "", "test.tmp", "", false);
            CloudBlockBlob cloudBlockBlob = _container.GetBlockBlobReference("test.tmp");
            _mockCloudBlockBlobWrapper.Verify(b => b.UploadFromStreamAsync(It.Is<CloudBlockBlob>(c => c.Uri == cloudBlockBlob.Uri), It.IsAny<Stream>()), Times.Once);
        }

        [Test]
        public async Task UploadStreamChunkAsync_EmptyStorageUriAndFileNameEmpty_Exception()
        {
            Uri? emptyUri = null;

            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                        await _target.UploadStreamChunkAsync(emptyUri, BYTES, _startPosition, _checkSum, "", "", false));
        }

        [Test]
        public async Task UploadStreamChunkAsync_BlobExists_DownloadExistsData()
        {

            _mockCloudBlockBlobWrapper.Setup(b => b.BlobExists(It.IsAny<CloudBlockBlob>())).ReturnsAsync(true);
            _mockCloudBlockBlobWrapper.Setup(b => b.DownloadToStreamAsync(It.IsAny<CloudBlockBlob>())).ReturnsAsync(new MemoryStream(BYTES));
            await _target.UploadStreamChunkAsync(STORAGE_URI, BYTES, 0, "", "", "", false);
            _mockCloudBlockBlobWrapper.Verify(b => b.DownloadToStreamAsync(It.IsAny<CloudBlockBlob>()), Times.Once);
        }
        [Test]
        public async Task UploadStreamChunkAsync_NotRunDiagnostics_NotInvokeHandleDownloadForDiagnosticsAsync()
        {
            _mockCloudBlockBlobWrapper.Setup(b => b.BlobExists(It.IsAny<CloudBlockBlob>())).ReturnsAsync(true);
            _mockCloudBlockBlobWrapper.Setup(b => b.DownloadToStreamAsync(It.IsAny<CloudBlockBlob>())).ReturnsAsync(new MemoryStream(BYTES));
            _mockCheckSumService.Setup(c => c.CalculateCheckSumAsync(It.IsAny<Stream>(), It.IsAny<CheckSumType>())).ReturnsAsync("checkSum");
            await _target.UploadStreamChunkAsync(STORAGE_URI, BYTES, 0, "checkSum", "", "", false);

            var destionationPath = "C:\\Users\\Test\\AppData\\Local\\Temp\\test.tmp";

            _mockTwinDiseredService.Verify(b => b.AddDesiredRecipeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DownloadAction>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }


        [Test]
        public async Task UploadStreamChunkAsync_EndChunckInRunDiagnosticsCheckSumIsEqual_InvokeHandleDownloadForDiagnosticsAsync()
        {
            _mockCloudBlockBlobWrapper.Setup(b => b.BlobExists(It.IsAny<CloudBlockBlob>())).ReturnsAsync(true);
            _mockCloudBlockBlobWrapper.Setup(b => b.DownloadToStreamAsync(It.IsAny<CloudBlockBlob>())).ReturnsAsync(new MemoryStream(BYTES));
            _mockCheckSumService.Setup(c => c.CalculateCheckSumAsync(It.IsAny<Stream>(), It.IsAny<CheckSumType>())).ReturnsAsync("checkSum");
            await _target.UploadStreamChunkAsync(STORAGE_URI, BYTES, 0, "checkSum", "", "", true);

            var destionationPath = "C:\\Users\\Test\\AppData\\Local\\Temp\\test.tmp";

            _mockTwinDiseredService.Verify(b => b.AddDesiredRecipeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DownloadAction>(), It.IsAny<int>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task UploadStreamChunkAsync_EndChunckInRunDiagnosticsCheckSumIsNotEqual_NotInvokeHandleDownloadForDiagnosticsAsync()
        {
            _mockCloudBlockBlobWrapper.Setup(b => b.BlobExists(It.IsAny<CloudBlockBlob>())).ReturnsAsync(true);
            _mockCloudBlockBlobWrapper.Setup(b => b.DownloadToStreamAsync(It.IsAny<CloudBlockBlob>())).ReturnsAsync(new MemoryStream(BYTES));
            _mockCheckSumService.Setup(c => c.CalculateCheckSumAsync(It.IsAny<Stream>(), It.IsAny<CheckSumType>())).ReturnsAsync("checkSumSecond");
            await _target.UploadStreamChunkAsync(STORAGE_URI, BYTES, 0, "checkSum", "", "", true);

            var destionationPath = "C:\\Users\\Test\\AppData\\Local\\Temp\\test.tmp";

            _mockTwinDiseredService.Verify(b => b.AddDesiredRecipeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DownloadAction>(), It.IsAny<int>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task HandleDownloadForDiagnosticsAsync_DestinationPath_BuildDestionationBySource()
        {
            await _target.HandleDownloadForDiagnosticsAsync("", STORAGE_URI);

            var destionationPath = "C:\\Users\\Test\\AppData\\Local\\Temp\\test.tmp";

            _mockTwinDiseredService.Verify(b => b.AddDesiredRecipeAsync(It.IsAny<string>(), SharedConstants.CHANGE_SPEC_DIAGNOSTICS_NAME,
            It.Is<DownloadAction>(x => x.DestinationPath == destionationPath), It.IsAny<int>(), SharedConstants.DEFAULT_TRANSACTIONS_KEY), Times.Once);
        }
    }
}
