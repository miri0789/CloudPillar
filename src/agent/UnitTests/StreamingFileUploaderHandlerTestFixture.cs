using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using Moq;
using Shared.Entities.Services;
using Shared.Logger;

namespace CloudPillar.Agent.Tests
{
    [TestFixture]
    public class StreamingFileUploaderHandlerTestFixture
    {
        private Mock<IDeviceClientWrapper> _deviceClientMock;
        private Mock<ILoggerHandler> _loggerMock;
        private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
        private Mock<ICheckSumService> _checkSumServiceMock;
        private Mock<ITwinActionsHandler> _twinActionsHandler;
        private StreamingFileUploaderHandler _target;

        private const string ACTION_ID = "action123";
        private const int CHUNK_SIZE = 1024;
        private const int NUM_OF_CHUNKS = 4;
        private const string CORRELATION_ID = "abc";
        private readonly Uri STORAGE_URI = new Uri("https://mockstorage.example.com/mock-container");
        private ActionToReport actionToReport = new ActionToReport();

        [SetUp]
        public void Setup()
        {
            _deviceClientMock = new Mock<IDeviceClientWrapper>();
            _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
            _checkSumServiceMock = new Mock<ICheckSumService>();
            _twinActionsHandler = new Mock<ITwinActionsHandler>();
            _loggerMock = new Mock<ILoggerHandler>();

            _deviceClientMock.Setup(dc => dc.GetChunkSizeByTransportType()).Returns(CHUNK_SIZE);
            _d2CMessengerHandlerMock.Setup(d => d.SendStreamingUploadChunkEventAsync(It.IsAny<byte[]>(), It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.CompletedTask);

            _target = new StreamingFileUploaderHandler(_d2CMessengerHandlerMock.Object, _deviceClientMock.Object, _checkSumServiceMock.Object, _twinActionsHandler.Object, _loggerMock.Object);
        }
        [Test]
        public async Task SendStreamingUploadChunks_SingleChunk_CompleteTask()
        {
            var stream = CreateLargeStream(CHUNK_SIZE * 1);

            await _target.UploadFromStreamAsync(actionToReport, stream, STORAGE_URI, ACTION_ID, CORRELATION_ID, CancellationToken.None);

            _deviceClientMock.Verify(w => w.CompleteFileUploadAsync(It.IsAny<FileUploadCompletionNotification>(), CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task SendStreamingUploadChunks_LargeStream_VerifyChunkCalls()
        {


            var largeStream = CreateLargeStream(CHUNK_SIZE * NUM_OF_CHUNKS);
            await _target.UploadFromStreamAsync(actionToReport, largeStream, STORAGE_URI, ACTION_ID, CORRELATION_ID, CancellationToken.None);

            _d2CMessengerHandlerMock.Verify(w => w.SendStreamingUploadChunkEventAsync(It.IsAny<byte[]>(), It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Exactly(NUM_OF_CHUNKS));
        }
        public static Stream CreateLargeStream(long streamSize)
        {
            var random = new Random();
            var stream = new MemoryStream();

            // Define the chunk size

            // Generate random data and write it to the stream
            for (long i = 0; i < streamSize; i += CHUNK_SIZE)
            {
                var chunk = new byte[CHUNK_SIZE];
                random.NextBytes(chunk);
                stream.Write(chunk, 0, CHUNK_SIZE);
            }

            // Reset the stream position to the beginning
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
    }


}