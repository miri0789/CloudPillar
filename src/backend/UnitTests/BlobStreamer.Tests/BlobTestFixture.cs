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
using Backend.Infra.Common.Wrappers.Interfaces;

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
        private Mock<IDeviceClientWrapper> _mockDeviceClientWrapper;
        private Mock<ISendQueueMessagesService> _mockSendQueueMessagesService;
        private IBlobService _target;


        private const string _deviceId = "test-device";
        private const string _fileName = "test-file.txt";
        private const string _changeSpecId = "1.2.3";
        private const int _chunkSize = 1024;
        private const int _rangeSize = 4096;
        private const int _rangeIndex = 0;
        private const long _startPosition = 0;
        private const int _rangesCount = 0;
        private SignFileEvent signFileEvent;
        private const int _actionIndexd = 123;
        private const long _blobSize = 2048L;


        [SetUp]
        public void Setup()
        {
            _mockCloudStorageWrapper = new Mock<ICloudStorageWrapper>();
            _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
            _mockLogger = new Mock<ILoggerHandler>();
            _mockMessageFactory = new Mock<IMessageFactory>();
            _mockMessageFactory.Setup(c => c.PrepareC2DMessage(It.IsAny<C2DMessages>(), It.IsAny<int>())).Returns(new Message());
            _mockDeviceConnectService = new Mock<IDeviceConnectService>();
            _mockCheckSumService = new Mock<ICheckSumService>();
            _mockDeviceClientWrapper = new Mock<IDeviceClientWrapper>();
            _mockSendQueueMessagesService = new Mock<ISendQueueMessagesService>();
            var mockDeviceClient = new Mock<ServiceClient>();

            signFileEvent = new SignFileEvent
            {
                BufferSize = _chunkSize,
                FileName = _fileName
            };

            _mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://storageaccount/container/blob"));
            _mockCloudStorageWrapper.Setup(c => c.GetBlockBlobReference(It.IsAny<CloudBlobContainer>(), _fileName)).ReturnsAsync(_mockBlockBlob.Object);
            _target = new BlobService(_mockEnvironmentsWrapper.Object,
                _mockCloudStorageWrapper.Object, _mockDeviceConnectService.Object, _mockCheckSumService.Object, _mockLogger.Object, _mockMessageFactory.Object,
                _mockDeviceClientWrapper.Object, _mockSendQueueMessagesService.Object);

        }

        [Test]
        public async Task SendRangeByChunksAsync_OnCall_ShouldGetBlobLength()
        {
            _mockCloudStorageWrapper.Setup(c => c.GetBlobLength(It.IsAny<CloudBlockBlob>())).Returns(_rangeSize);
            await _target.SendRangeByChunksAsync(_deviceId, _changeSpecId, _fileName, _chunkSize, _rangeSize, _rangeIndex, _startPosition, 0, _rangesCount);
            _mockCloudStorageWrapper.Verify(c => c.GetBlobLength(It.IsAny<CloudBlockBlob>()), Times.Once);
        }

        [Test]
        public async Task SendFileDownloadAsync_OnCall_SendMessageToQueue()
        {
            await _target.SendFileDownloadAsync(_deviceId, new FileDownloadEvent
            {
                FileName = _fileName,
                ChunkSize = _chunkSize,
                StartPosition = 0,
                ActionIndex = _actionIndexd,
                ChangeSpecId = _changeSpecId
            });

            for (long offset = _startPosition, rangeIndex = 0; offset < _blobSize; offset += _rangeSize, rangeIndex++)
            {
                _mockSendQueueMessagesService.Verify(q =>
                    q.SendMessageToQueue(It.IsAny<string>(), It.IsAny<object>()), Times.Once);
            }
        }


        [Test]
        public async Task SendFileDownloadAsync__MsgWithEndPosition_SendsOneRange()
        {
            await _target.SendFileDownloadAsync(_deviceId, new FileDownloadEvent
            {
                FileName = _fileName,
                ChunkSize = _chunkSize,
                StartPosition = 0,
                ActionIndex = _actionIndexd,
                EndPosition = 123,
                ChangeSpecId = _changeSpecId
            });

            _mockSendQueueMessagesService.Verify(q =>
                       q.SendMessageToQueue(It.IsAny<string>(), It.IsAny<object>()), Times.Once);

        }

        [Test]
        public async Task SendFileDownloadAsync_GetBlobSizeFails_SendErrMsg()
        {
            var errMsg = "Failed to retrieve blob size.";
            _mockCloudStorageWrapper.Setup(c => c.GetBlockBlobReference(It.IsAny<CloudBlobContainer>(), It.IsAny<string>())).ThrowsAsync(new StorageException(errMsg, null));

            await _target.SendFileDownloadAsync(_deviceId, new FileDownloadEvent
            {
                FileName = _fileName,
                ChunkSize = _chunkSize,
                StartPosition = 0,
                ActionIndex = _actionIndexd,
                ChangeSpecId = _changeSpecId
            });
            var url = $"blob/RangeError?deviceId={_deviceId}&fileName={_fileName}&actionIndex={_actionIndexd}&error=Failed to retrieve blob size.&changeSpecId={_changeSpecId}";
            _mockSendQueueMessagesService.Verify(q =>
                q.SendMessageToQueue(It.Is<string>(url => url == url), null), Times.Once);
        }

        [Test]
        public async Task SendRangeByChunksAsync_ValidRange_GetRangeCheckSum()
        {
            _mockCheckSumService.Setup(b => b.CalculateCheckSumAsync(It.IsAny<byte[]>(), It.IsAny<CheckSumType>()));
            _mockCloudStorageWrapper.Setup(c => c.GetBlobLength(It.IsAny<CloudBlockBlob>())).Returns(_rangeSize);

            await _target.SendRangeByChunksAsync(_deviceId, _changeSpecId, _fileName, _chunkSize, _rangeSize, _rangeIndex, _startPosition, 0, _rangesCount);
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


        [Test]
        public async Task CalculateHashAsync_onCall_ShouldReardTheFile()
        {
            _mockCloudStorageWrapper.Setup(c => c.GetBlobLength(It.IsAny<CloudBlockBlob>())).Returns(_rangeSize);
            var result = await _target.CalculateHashAsync(_fileName, signFileEvent);
            _mockCloudStorageWrapper.Verify(b => b.GetBlockBlobReference(It.IsAny<CloudBlobContainer>(), _fileName), Times.Once);
            _mockBlockBlob.Verify(b => b.DownloadRangeToByteArrayAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<long>()), Times.AtLeast(1));
        }

        [Test]
        public async Task CalculateHashAsync_OnCall_ShouldReturnHash()
        {
            _mockCloudStorageWrapper.Setup(c => c.GetBlobLength(It.IsAny<CloudBlockBlob>())).Returns(_rangeSize);
            var result = await _target.CalculateHashAsync(_fileName, signFileEvent);
            Assert.IsNotNull(result);
        }

        [Test]
        public async Task CalculateHashAsync_OnNoLength_ShouldnotDownloadRange()
        {
            var result = await _target.CalculateHashAsync(_fileName, signFileEvent);
            _mockCloudStorageWrapper.Verify(b => b.GetBlockBlobReference(It.IsAny<CloudBlobContainer>(), _fileName), Times.Once);
            _mockBlockBlob.Verify(b => b.DownloadRangeToByteArrayAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<long>()), Times.Never());
        }
    }
}
