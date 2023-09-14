using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Moq;
using Microsoft.Azure.Devices.Client;
using System.Text;
using Newtonsoft.Json;
using Shared.Logger;
using Microsoft.Azure.Devices.Client.Transport;
using Shared.Entities.Events;

namespace CloudPillar.Agent.Tests
{
    [TestFixture]
    public class D2CMessengerHandlerTestFixture
    {

        private Mock<IDeviceClientWrapper> _deviceClientMock;
        private Mock<ILoggerHandler> _loggerMock;
        private ID2CMessengerHandler _target;

        private const string FILE_NAME = "fileName.txt";
        private const string ACTION_ID = "action123";
        private const long START_POSITION = 10;
        private const long END_POSITION = 20;
        private const int KB = 1024;
        private const int MQQT_KB = 32 * KB;
        private const int AMQP_KB = 64 * KB;
        private const int HTTP1_KB = 256 * KB;
        private const string CORRELATION_ID = "abc";
        private Uri STORAGE_URI = new Uri("https://nechama.blob.core.windows.net/nechama-container");
        private MemoryStream READ_STREAM = new MemoryStream(new byte[] { 1, 2, 3 });
        private const int CHUNK_INDEX = 1;

        [SetUp]
        public void Setup()
        {
            _deviceClientMock = new Mock<IDeviceClientWrapper>();
            _loggerMock = new Mock<ILoggerHandler>();

            _target = new D2CMessengerHandler(_deviceClientMock.Object, _loggerMock.Object);

        }

        [TestCase(TransportType.Mqtt, MQQT_KB)]
        [TestCase(TransportType.Amqp, AMQP_KB)]
        [TestCase(TransportType.Http1, HTTP1_KB)]
        [TestCase((TransportType)100, 32 * KB)] // Unknown transport type
        public async Task SendFirmwareUpdateEventAsync_ByTransportType_SendCorrectChunkSize(TransportType transportType, int expectedChunkSize)
        {
            _deviceClientMock.Setup(dc => dc.GetChunkSizeByTransportType()).Returns(expectedChunkSize);

            await _target.SendFirmwareUpdateEventAsync(FILE_NAME, ACTION_ID, START_POSITION, END_POSITION);
            _deviceClientMock.Verify(dc => dc.SendEventAsync(It.Is<Message>(msg => CheckMessageContent(msg, expectedChunkSize, FILE_NAME, ACTION_ID, START_POSITION, END_POSITION) == true)), Times.Once);
        }



        [Test]
        public async Task SendFirmwareUpdateEventAsync_Failure_ThrowException()
        {
            _deviceClientMock.Setup(dc => dc.GetChunkSizeByTransportType()).Returns(MQQT_KB);

            _deviceClientMock.Setup(dc => dc.SendEventAsync(It.IsAny<Message>())).ThrowsAsync(new Exception());

            Assert.ThrowsAsync<Exception>(async () =>
            {
                await _target.SendFirmwareUpdateEventAsync(FILE_NAME, ACTION_ID);
            });
        }

        [Test]
        public async Task SendStreamingUploadChunkEventAsync_ValidData_CompleteFileUpload()
        {

            FileUploadCompletionNotification notification = InitializeNotification(true);
            _deviceClientMock.Setup(dc => dc.GetChunkSizeByTransportType()).Returns(MQQT_KB);

            _deviceClientMock.Setup(dc => dc.CompleteFileUploadAsync(notification, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            await _target.SendStreamingUploadChunkEventAsync(READ_STREAM, STORAGE_URI, ACTION_ID, CORRELATION_ID, START_POSITION);
            _deviceClientMock.Verify(dc => dc.CompleteFileUploadAsync(It.IsAny<FileUploadCompletionNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SendStreamingUploadChunkEventAsync_ValidData_SendEventAsync()
        {
            var chunkIndex = CHUNK_INDEX;
            var currentPosition = 0;
            var totalChunks = CalculateTotalChunks(READ_STREAM.Length, MQQT_KB);
            _deviceClientMock.Setup(dc => dc.GetChunkSizeByTransportType()).Returns(MQQT_KB);
            var chunkSize = _deviceClientMock.Object.GetChunkSizeByTransportType();

            await _target.SendStreamingUploadChunkEventAsync(READ_STREAM, STORAGE_URI, ACTION_ID, CORRELATION_ID, START_POSITION);

            _deviceClientMock.Verify(dc => dc.SendEventAsync(It.IsAny<Message>()), Times.Exactly(totalChunks));
            //      currentPosition, END_POSITION) == true)), Times.Once);
            // while (currentPosition < READ_STREAM.Length)
            // {
            //     var remainingBytes = READ_STREAM.Length - currentPosition;
            //     var bytesToUpload = Math.Min(chunkSize, remainingBytes);

            //     var buffer = new byte[bytesToUpload];
            //     await READ_STREAM.ReadAsync(buffer, currentPosition, (int)bytesToUpload);

            //     _deviceClientMock.Verify(dc => dc.SendEventAsync(It.Is<Message>(msg => CheckUploadMessageContent(msg, chunkIndex, STORAGE_URI, ACTION_ID, buffer,
            //      currentPosition, END_POSITION) == true)), Times.Once);

            //     currentPosition += chunkSize;
            //     chunkIndex++;
            // }

        }


        private bool CheckMessageContent(Message msg, int chunkSize, string fileName, string actionId, long? startPosition, long? endPosition)
        {
            string messageString = Encoding.ASCII.GetString(msg.GetBytes());
            FirmwareUpdateEvent firmwareUpdateEvent = JsonConvert.DeserializeObject<FirmwareUpdateEvent>(messageString);
            return firmwareUpdateEvent.ChunkSize == chunkSize &&
                  firmwareUpdateEvent.StartPosition == startPosition &&
                  firmwareUpdateEvent.FileName == fileName &&
                  firmwareUpdateEvent.ActionId == actionId &&
                  firmwareUpdateEvent.EndPosition == endPosition;
        }

        private bool CheckUploadMessageContent(Message msg, int chunkIndex, Uri storageUri, string actionId, byte[] data, long? startPosition, long? endPosition)
        {
            string messageString = Encoding.ASCII.GetString(msg.GetBytes());
            StreamingUploadChunkEvent streamingUploadChunkEvent = JsonConvert.DeserializeObject<StreamingUploadChunkEvent>(messageString);
            var comp = streamingUploadChunkEvent.ChunkIndex == chunkIndex &&
                  streamingUploadChunkEvent.ActionId == actionId &&
                  streamingUploadChunkEvent.StartPosition == startPosition &&
                  streamingUploadChunkEvent.StorageUri == storageUri &&
                  streamingUploadChunkEvent.Data == data;
            return comp;
        }

        private FileUploadCompletionNotification InitializeNotification(bool isSuccess)
        {
            return new FileUploadCompletionNotification()
            {
                IsSuccess = isSuccess,
                CorrelationId = CORRELATION_ID
            };
        }

        private int CalculateTotalChunks(long streamLength, int chunkSize)
        {
            return (int)Math.Ceiling((double)streamLength / chunkSize);
        }
    }
}