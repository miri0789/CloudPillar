﻿using Backend.BlobStreamer.Services;
using Microsoft.Azure.Storage.Blob;
using Moq;
using Shared.Logger;
using Shared.Entities.Services;
using Backend.BlobStreamer.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Backend.BlobStreamer.Services.Interfaces;
using Shared.Entities.Twin;

namespace Backend.BlobStreamer.Tests
{

    [TestFixture]
    public class UploadStreamChunksServiceTestFixture
    {
        private Mock<ICheckSumService> _mockCheckSumService;
        private Mock<ICloudBlockBlobWrapper> _mockCloudBlockBlobWrapper;
        private Mock<ITwinDiseredService> _mockTwinDiseredService;
        private Mock<ILoggerHandler> _mockLogger;
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

            createMockBlob(STORAGE_URI);
            _mockCloudBlockBlobWrapper.Setup(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>())).Returns(Task.CompletedTask);

            _target = new UploadStreamChunksService(_mockLogger.Object, _mockCheckSumService.Object, _mockCloudBlockBlobWrapper.Object, _mockTwinDiseredService.Object);
        }

        private void createMockBlob(Uri storageUri)
        {
            var _mockBlockBlob = new Mock<CloudBlockBlob>(storageUri);
            _mockCloudBlockBlobWrapper.Setup(b => b.CreateCloudBlockBlob(It.IsAny<Uri>())).Returns(_mockBlockBlob.Object);
        }

        [Test]
        public async Task UploadStreamChunkAsync_ValidData_Success()
        {

            await _target.UploadStreamChunkAsync(STORAGE_URI, BYTES, 0, "", "", false);
            _mockCloudBlockBlobWrapper.Verify(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>()), Times.Once);
        }

        [Test]
        public async Task UploadStreamChunkAsync_EmptyStorageUri_NotUpload()
        {
            Uri? emptyUri = null;

            await _target.UploadStreamChunkAsync(emptyUri, BYTES, _startPosition, _checkSum, "", false);
            _mockCloudBlockBlobWrapper.Verify(b => b.UploadFromStreamAsync(It.IsAny<CloudBlockBlob>(), It.IsAny<Stream>()), Times.Never);
        }

        [Test]
        public async Task UploadStreamChunkAsync_BlobExists_DownloadExistsData()
        {

            _mockCloudBlockBlobWrapper.Setup(b => b.BlobExists(It.IsAny<CloudBlockBlob>())).ReturnsAsync(true);

            await _target.UploadStreamChunkAsync(STORAGE_URI, BYTES, 0, "", "", false);
            _mockCloudBlockBlobWrapper.Verify(b => b.DownloadToStreamAsync(It.IsAny<CloudBlockBlob>()), Times.Once);
        }

        [Test]
        public async Task HandleDownloadForDiagnosticsAsync_DestinationPath_BuildDestionationBySource()
        {
            await _target.HandleDownloadForDiagnosticsAsync("", STORAGE_URI);

            var destionationPath = "C:\\Users\\Test\\AppData\\Local\\Temp\\test.tmp";

            _mockTwinDiseredService.Verify(b => b.AddDesiredRecipeAsync(It.IsAny<string>(), TwinPatchChangeSpec.Diagnostics,
            It.Is<DownloadAction>(x => x.DestinationPath == destionationPath)), Times.Once);
        }
    }
}
