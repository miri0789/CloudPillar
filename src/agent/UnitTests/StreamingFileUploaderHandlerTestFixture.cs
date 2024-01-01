using CloudPillar.Agent.Entities;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client.Transport;
using Moq;
using Shared.Entities.Services;
using Shared.Entities.Twin;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;

namespace CloudPillar.Agent.Tests
{
    [TestFixture]
    public class StreamingFileUploaderHandlerTestFixture
    {
        private Mock<IDeviceClientWrapper> _deviceClientMock;
        private Mock<ILoggerHandler> _loggerMock;
        private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
        private Mock<ICheckSumService> _checkSumServiceMock;
        private Mock<ITwinReportHandler> _twinReportHandler;
        private StreamingFileUploaderHandler _target;

        private const string CORRELATION_ID = "correlation123";
        private const string FILE_NAME = "filename.txt";
        private const int CHUNK_SIZE = 1024;
        private const int NUM_OF_CHUNKS = 5;
        private const int PROGRESS_PERCENTAGE = 10;
        private readonly Uri STORAGE_URI = new Uri("https://mockstorage.example.com/mock-container");
        private ActionToReport actionToReport = new ActionToReport();
        private FileUploadCompletionNotification notification = new FileUploadCompletionNotification();
        private Mock<IOptions<UploadCompleteRetrySettings>> _uploadCompleteRetrySettingsMock;

        [SetUp]
        public void Setup()
        {
            _deviceClientMock = new Mock<IDeviceClientWrapper>();
            _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
            _checkSumServiceMock = new Mock<ICheckSumService>();
            _twinReportHandler = new Mock<ITwinReportHandler>();
            _loggerMock = new Mock<ILoggerHandler>();
            _uploadCompleteRetrySettingsMock = new Mock<IOptions<UploadCompleteRetrySettings>>();
            _uploadCompleteRetrySettingsMock.Setup(s => s.Value).Returns(new UploadCompleteRetrySettings { });

            _deviceClientMock.Setup(dc => dc.GetChunkSizeByTransportType()).Returns(CHUNK_SIZE);
            _d2CMessengerHandlerMock.Setup(d => d.SendStreamingUploadChunkEventAsync(It.IsAny<byte[]>(), It.IsAny<Uri>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>())).Returns(Task.CompletedTask);
            _twinReportHandler.Setup(rep => rep.GetActionToReport(It.IsAny<ActionToReport>(), It.IsAny<string>())).Returns(new TwinActionReported());
            CreateTarget();
        }

        [Test]
        public async Task SendStreamingUploadChunks_SingleChunk_CompleteTask()
        {
            actionToReport.UploadCompleted = false;
            actionToReport.TwinReport.Progress = 0;
            var stream = CreateStream(CHUNK_SIZE * 1);

            await _target.UploadFromStreamAsync(notification, actionToReport, stream, STORAGE_URI, CORRELATION_ID, FILE_NAME, CancellationToken.None);

            _deviceClientMock.Verify(w => w.CompleteFileUploadAsync(It.IsAny<FileUploadCompletionNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SendStreamingUploadChunks_InprogressUpload_CompleteUploadOnlyOnce()
        {
            actionToReport.UploadCompleted = false;
            actionToReport.TwinReport.Progress = PROGRESS_PERCENTAGE;
            var largeStream = CreateStream(CHUNK_SIZE * NUM_OF_CHUNKS);
            await _target.UploadFromStreamAsync(notification, actionToReport, largeStream, STORAGE_URI, CORRELATION_ID, FILE_NAME, CancellationToken.None);

            _deviceClientMock.Verify(w => w.CompleteFileUploadAsync(It.IsAny<FileUploadCompletionNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SendStreamingUploadChunks_LargeStream_VerifyChunkCalls()
        {
            var largeStream = CreateStream(CHUNK_SIZE * NUM_OF_CHUNKS);
            actionToReport.TwinReport.Progress = PROGRESS_PERCENTAGE;

            await _target.UploadFromStreamAsync(notification, actionToReport, largeStream, STORAGE_URI, CORRELATION_ID, FILE_NAME, CancellationToken.None);

            _d2CMessengerHandlerMock.Verify(w => w.SendStreamingUploadChunkEventAsync(It.IsAny<byte[]>(), It.IsAny<Uri>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Exactly(NUM_OF_CHUNKS));

        }

        [Test]
        public async Task SendStreamingUploadChunks_InprogressStatus_UploadLeftChuncks()
        {
            var stream = CreateStream(CHUNK_SIZE * NUM_OF_CHUNKS);
            actionToReport.TwinReport.Status = StatusType.InProgress;
            actionToReport.TwinReport.Progress = PROGRESS_PERCENTAGE;

            await _target.UploadFromStreamAsync(notification, actionToReport, stream, STORAGE_URI, CORRELATION_ID, FILE_NAME, CancellationToken.None);

            var uplodedChuncks = CalculateCurrentPosition(stream.Length, PROGRESS_PERCENTAGE) / CHUNK_SIZE;
            var leftCHhuncks = NUM_OF_CHUNKS - uplodedChuncks;

            _d2CMessengerHandlerMock.Verify(w => w.SendStreamingUploadChunkEventAsync(It.IsAny<byte[]>(), It.IsAny<Uri>(),
             It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.Exactly(leftCHhuncks));
        }

        [Test]
        public async Task SendStreamingUploadChunks_CompleteFileUploadAsync_ShouldRetry()
        {
            var maxRetries = 3;
            _uploadCompleteRetrySettingsMock.Setup(s => s.Value).Returns(new UploadCompleteRetrySettings { MaxRetries = maxRetries, DelaySeconds = 1 });
            CreateTarget();

            actionToReport.TwinReport.Progress = 0;
            var stream = CreateStream(CHUNK_SIZE * 1);

            _deviceClientMock
                .SetupSequence(s => s.CompleteFileUploadAsync(It.IsAny<FileUploadCompletionNotification>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("First send failed"))
                .Throws(new Exception("Second send failed"))
                .Returns(Task.CompletedTask);

            await _target.UploadFromStreamAsync(notification, actionToReport, stream, STORAGE_URI, CORRELATION_ID, FILE_NAME, CancellationToken.None);
            _deviceClientMock.Verify(s => s.CompleteFileUploadAsync(It.IsAny<FileUploadCompletionNotification>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        private int CalculateCurrentPosition(float streamLength, float progressPercent)
        {
            int currentPosition = (int)Math.Floor(progressPercent * (float)streamLength / 100);

            Console.WriteLine($"Current Position: {currentPosition} bytes");
            return currentPosition;
        }


        private static Stream CreateStream(long streamSize)
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

        private void CreateTarget()
        {
            _target = new StreamingFileUploaderHandler(_d2CMessengerHandlerMock.Object, _deviceClientMock.Object, _checkSumServiceMock.Object, _twinReportHandler.Object, _uploadCompleteRetrySettingsMock.Object, _loggerMock.Object);
        }
    }


}