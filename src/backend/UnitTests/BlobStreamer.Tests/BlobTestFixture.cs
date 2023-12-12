using Backend.BlobStreamer.Services;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Moq;
using Shared.Entities.Factories;
using Shared.Entities.Messages;
using Shared.Logger;
using Shared.Entities.Services;
using Shared.Enums;
using Backend.BlobStreamer.Wrappers.Interfaces;
using Backend.Infra.Common.Services.Interfaces;
using Backend.BlobStreamer.Services.Interfaces;

namespace Backend.BlobStreamer.Tests
{

    [TestFixture]
    public class BlobTestFixture
    {
        private Mock<ICloudStorageWrapper> _mockCloudStorageWrapper;
        private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
        private Mock<CloudBlockBlob> _mockBlockBlob;
        private Mock<ILoggerHandler> _mockLogger;
        private Mock<IMessageFactory> _mockMessageFactory;
        private Mock<IDeviceConnectService> _mockDeviceConnectService;
        private Mock<ICheckSumService> _mockCheckSumService;
        private IBlobService _target;


        private const string _deviceId = "test-device";
        private const string _fileName = "test-file.txt";
        private const int _chunkSize = 1024;
        private const int _rangeSize = 4096;
        private const int _rangeIndex = 0;
        private const long _startPosition = 0;
        private const int _rangesCount = 0;

        [SetUp]
        public void Setup()
        {
            _mockCloudStorageWrapper = new Mock<ICloudStorageWrapper>();
            _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
            _mockLogger = new Mock<ILoggerHandler>();
            _mockMessageFactory = new Mock<IMessageFactory>();
            _mockMessageFactory.Setup(c => c.PrepareC2DMessage(It.IsAny<C2DMessages>(), It.IsAny<int>())).Returns(new Message());
            _mockEnvironmentsWrapper.Setup(c => c.retryPolicyExponent).Returns(3);
            _mockDeviceConnectService = new Mock<IDeviceConnectService>();
            _mockCheckSumService = new Mock<ICheckSumService>();
            var mockDeviceClient = new Mock<ServiceClient>();

            _mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://storageaccount/container/blob"));
            _mockCloudStorageWrapper.Setup(c => c.GetBlockBlobReference(It.IsAny<CloudBlobContainer>(), _fileName)).ReturnsAsync(_mockBlockBlob.Object);
            _mockCloudStorageWrapper.Setup(c => c.GetBlobLength(It.IsAny<CloudBlockBlob>())).Returns(_rangeSize);
            _target = new BlobService(_mockEnvironmentsWrapper.Object,
                _mockCloudStorageWrapper.Object, _mockDeviceConnectService.Object, _mockCheckSumService.Object, _mockLogger.Object, _mockMessageFactory.Object);

        }


        [Test]
        public async Task SendRangeByChunksAsync_ShouldSendBlobMessages()
        {
            _mockBlockBlob.Setup(b => b.DownloadRangeToByteArrayAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>()));

            _mockDeviceConnectService.Setup(s => s.SendDeviceMessagesAsync(It.IsAny<Message[]>(), _deviceId)).Returns(Task.CompletedTask);
            await _target.SendRangeByChunksAsync(_deviceId, _fileName, _chunkSize, _rangeSize, _rangeIndex, _startPosition, new Guid().ToString(), _rangesCount);
            _mockDeviceConnectService.Verify(s => s.SendDeviceMessagesAsync(
                                                It.Is<Message[]>(messages => messages.Length == 4),
                                                _deviceId),
                                                Times.Once);
        }

        [Test]
        public async Task SendRangeByChunksAsync_ValidRange_GetRangeCheckSum()
        {
            _mockCheckSumService.Setup(b => b.CalculateCheckSumAsync(It.IsAny<byte[]>(), It.IsAny<CheckSumType>()));

            await _target.SendRangeByChunksAsync(_deviceId, _fileName, _chunkSize, _rangeSize, _rangeIndex, _startPosition, new Guid().ToString(), _rangesCount);
            _mockCheckSumService.Verify(s => s.CalculateCheckSumAsync(It.Is<byte[]>(b => b.Length == _rangeSize), It.IsAny<CheckSumType>()), Times.Once);
        }

        [Test]
        public async Task GetBlobMetadataAsync_ShouldReturnBlobProperties()
        {
            var result = await _target.GetBlobMetadataAsync(_fileName);
            _mockCloudStorageWrapper.Verify(b => b.GetBlockBlobReference(It.IsAny<CloudBlobContainer>(), _fileName), Times.Once);
        }


        [Test]
        public async Task GetBlobMetadataAsync_FileNotExists_ReturnsNull()
        {
            string fileName = "nonexistent-file.txt";
            _mockCloudStorageWrapper.Setup(c => c.GetBlockBlobReference(It.IsAny<CloudBlobContainer>(), _fileName))
                .ThrowsAsync(new StorageException("The specified blob does not exist.", null));

            async Task GetBlobMetadataAsync() => await _target.GetBlobMetadataAsync(fileName);
            Assert.ThrowsAsync<NullReferenceException>(GetBlobMetadataAsync);
        }
        
    }
}
