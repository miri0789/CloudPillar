using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using Moq;
using Shared.Logger;

namespace CloudPillar.Agent.Tests
{
    [TestFixture]
    public class StreamingFileUploaderHandlerTestFixture
    {

        private Mock<IDeviceClientWrapper> _deviceClientMock;
        private Mock<ILoggerHandler> _loggerMock;
        private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
        private StreamingFileUploaderHandler _target;

        private const string ACTION_ID = "action123";
        private const int MQQT_KB = 32 * 1024;
        private const string CORRELATION_ID = "abc";
        private Uri STORAGE_URI = new Uri("https://nechama.blob.core.windows.net/nechama-container");
        private MemoryStream READ_STREAM = new MemoryStream(new byte[] { 1, 2, 3 });
        private int totalChunks;

        [SetUp]
        public void Setup()
        {
            _deviceClientMock = new Mock<IDeviceClientWrapper>();
            _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
            _loggerMock = new Mock<ILoggerHandler>();

            _deviceClientMock.Setup(dc => dc.GetChunkSizeByTransportType()).Returns(MQQT_KB);
            _d2CMessengerHandlerMock.Setup(d => d.SendStreamingUploadChunkEventAsync(It.IsAny<byte[]>(), It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<long>())).Returns(Task.CompletedTask);

            _target = new StreamingFileUploaderHandler(_d2CMessengerHandlerMock.Object, _deviceClientMock.Object, _loggerMock.Object);

            totalChunks = (int)Math.Ceiling((double)READ_STREAM.Length / MQQT_KB);
        }

        [Test]
        public async Task SendStreamingUploadChunks_SendChunks_VerifyChunkCalls()
        {
            await _target.UploadFromStreamAsync(READ_STREAM, STORAGE_URI, ACTION_ID, CORRELATION_ID, CancellationToken.None);

            _d2CMessengerHandlerMock.Verify(h => h.SendStreamingUploadChunkEventAsync(It.IsAny<byte[]>(), STORAGE_URI, ACTION_ID, It.IsAny<long>()), Times.Exactly(totalChunks));
        }

        [Test]
        public async Task SendStreamingUploadChunks_SendChunks_CompleteTask()
        {
            await _target.UploadFromStreamAsync(READ_STREAM, STORAGE_URI, ACTION_ID, CORRELATION_ID, CancellationToken.None);

            _deviceClientMock.Verify(w => w.CompleteFileUploadAsync(It.IsAny<FileUploadCompletionNotification>(), CancellationToken.None), Times.Once);
        }
    }
}