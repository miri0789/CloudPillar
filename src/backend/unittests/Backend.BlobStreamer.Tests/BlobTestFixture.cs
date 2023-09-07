using Backend.BlobStreamer.Interfaces;
using Backend.BlobStreamer.Services;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Moq;
using Shared.Entities.Factories;
using Shared.Entities.Messages;
using Shared.Logger;
using System.Reflection;
using Shared.Logger;
using Shared.Entities.Messages;
using Shared.Entities.Factories;

namespace Backend.BlobStreamer.Tests
{

    [TestFixture]
    public class BlobTestFixture
    {
        private Mock<ICloudStorageWrapper> _mockCloudStorageWrapper;
        private Mock<IDeviceClientWrapper> _mockDeviceClientWrapper;
        private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
        private Mock<CloudBlockBlob> _mockBlockBlob;
        private Mock<ILoggerHandler> _mockLogger;
        private Mock<IMessageFactory> _mockMessageFactory;
        private IBlobService _target;


        private const string _deviceId = "test-device";
        private const string _fileName = "test-file.txt";
        private const int _chunkSize = 1024;
        private const int _rangeSize = 4096;
        private const int _rangeIndex = 0;
        private const long _startPosition = 0;

        [SetUp]
        public void Setup()
        {
            _mockCloudStorageWrapper = new Mock<ICloudStorageWrapper>();
            _mockDeviceClientWrapper = new Mock<IDeviceClientWrapper>();
            _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
            _mockLogger = new Mock<ILoggerHandler>();
            _mockMessageFactory = new Mock<IMessageFactory>();
            _mockMessageFactory.Setup(c => c.PrepareBlobMessage(It.IsAny<BaseMessage>(), It.IsAny<int>())).Returns(new Message());
            _mockEnvironmentsWrapper.Setup(c => c.retryPolicyExponent).Returns(3);
            _mockDeviceClientWrapper.Setup(c => c.CreateFromConnectionString(It.IsAny<string>()))
            .Returns(new ServiceClient());

            _mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://storageaccount/container/blob"));
            _mockCloudStorageWrapper.Setup(c => c.GetBlockBlobReference(It.IsAny<CloudBlobContainer>(), _fileName)).ReturnsAsync(_mockBlockBlob.Object);
            _target = new BlobService(_mockEnvironmentsWrapper.Object,
                _mockCloudStorageWrapper.Object, _mockDeviceClientWrapper.Object, _mockLogger.Object, _mockMessageFactory.Object);

        }


        [Test]
        public async Task SendRangeByChunksAsync_ShouldSendBlobMessages()
        {
            _mockBlockBlob.Setup(b => b.DownloadRangeToByteArrayAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>()));

            _mockDeviceClientWrapper.Setup(s => s.SendAsync(It.IsAny<ServiceClient>(), _deviceId, It.IsAny<Message>())).Returns(Task.CompletedTask);
            await _target.SendRangeByChunksAsync(_deviceId, _fileName, _chunkSize, _rangeSize, _rangeIndex, _startPosition, new Guid().ToString(), _rangeSize);
            _mockDeviceClientWrapper.Verify(s => s.SendAsync(It.IsAny<ServiceClient>(), _deviceId, It.IsAny<Message>()), Times.Exactly(4));
            _mockMessageFactory.Verify(s => s.PrepareBlobMessage(It.IsAny<DownloadBlobChunkMessage>(), It.IsAny<int>()), Times.Exactly(4));
        }

        [Test]
        public async Task GetBlobMetadataAsync_ShouldReturnBlobProperties()
        {   
            var result = await _target.GetBlobMetadataAsync(_fileName);
            _mockCloudStorageWrapper.Verify(b => b.GetBlockBlobReference(It.IsAny<CloudBlobContainer>(), _fileName), Times.Once);
        }


        [Test]
        public async Task SendMessage_ShouldRetryAndSucceed()
        {
            _mockBlockBlob.Setup(b => b.DownloadRangeToByteArrayAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>()));

            _mockDeviceClientWrapper
                .SetupSequence(s => s.SendAsync(It.IsAny<ServiceClient>(), _deviceId, It.IsAny<Message>()))
                .Throws(new Exception("First send failed"))
                .Throws(new Exception("Second send failed"))
                .Returns(Task.CompletedTask);

            await _target.SendRangeByChunksAsync(_deviceId, _fileName, _chunkSize, _chunkSize, _rangeIndex, _startPosition, new Guid().ToString(), _rangeSize);
            _mockDeviceClientWrapper.Verify(s => s.SendAsync(It.IsAny<ServiceClient>(), _deviceId, It.IsAny<Message>()), Times.Exactly(3));
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
