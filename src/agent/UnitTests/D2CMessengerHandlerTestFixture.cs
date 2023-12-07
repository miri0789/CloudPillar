using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Moq;
using Microsoft.Azure.Devices.Client;
using System.Text;
using Newtonsoft.Json;
using CloudPillar.Agent.Handlers.Logger;
using Shared.Entities.Messages;
using System.Security.Cryptography;

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
        private readonly Uri STORAGE_URI = new Uri("https://mockstorage.example.com/mock-container");
        private MemoryStream READ_STREAM = new MemoryStream(new byte[] { 1, 2, 3 });
        private string checkSum;

        [SetUp]
        public void Setup()
        {
            checkSum = CalculateMdsCheckSumAsync(READ_STREAM).Result;
            _deviceClientMock = new Mock<IDeviceClientWrapper>();
            _loggerMock = new Mock<ILoggerHandler>();

            _target = new D2CMessengerHandler(_deviceClientMock.Object, _loggerMock.Object);

        }

        [TestCase(TransportType.Mqtt, MQQT_KB)]
        [TestCase(TransportType.Amqp, 64 * KB)]
        [TestCase(TransportType.Http1, 256 * KB)]
        [TestCase((TransportType)100, 32 * KB)] // Unknown transport type
        public async Task SendFirmwareUpdateEventAsync_ByTransportType_SendCorrectChunkSize(TransportType transportType, int expectedChunkSize)
        {
            _deviceClientMock.Setup(dc => dc.GetChunkSizeByTransportType()).Returns(expectedChunkSize);

            await _target.SendFirmwareUpdateEventAsync(CancellationToken.None, FILE_NAME, ACTION_ID, START_POSITION, END_POSITION);
            _deviceClientMock.Verify(dc => dc.SendEventAsync(It.Is<Message>(msg => CheckMessageContent(msg, expectedChunkSize, FILE_NAME, ACTION_ID, START_POSITION, END_POSITION) == true)), Times.Once);
        }

        [Test]
        public async Task SendFirmwareUpdateEventAsync_Failure_ThrowException()
        {
            _deviceClientMock.Setup(dc => dc.GetChunkSizeByTransportType()).Returns(MQQT_KB);

            _deviceClientMock.Setup(dc => dc.SendEventAsync(It.IsAny<Message>())).ThrowsAsync(new Exception());

            Assert.ThrowsAsync<Exception>(async () =>
            {
                await _target.SendFirmwareUpdateEventAsync(CancellationToken.None, FILE_NAME, ACTION_ID);
            });
        }

        [Test]
        public async Task SendStreamingUploadChunkEventAsync_Success_CompleteFileUpload()
        {
            _deviceClientMock.Setup(dc => dc.GetChunkSizeByTransportType()).Returns(MQQT_KB);

            _deviceClientMock.Setup(dc => dc.SendEventAsync(It.IsAny<Message>())).Returns(Task.CompletedTask);

            await _target.SendStreamingUploadChunkEventAsync(READ_STREAM.ToArray(), STORAGE_URI, ACTION_ID, START_POSITION, checkSum, CancellationToken.None);
            _deviceClientMock.Verify(dc => dc.SendEventAsync(It.IsAny<Message>()), Times.Once);
        }

        [Test]
        public async Task SendStreamingUploadChunks_Failure_ThrowException()
        {
            _deviceClientMock.Setup(dc => dc.GetChunkSizeByTransportType()).Returns(MQQT_KB);

            _deviceClientMock.Setup(dc => dc.SendEventAsync(It.IsAny<Message>())).ThrowsAsync(new Exception());

            Assert.ThrowsAsync<Exception>(async () =>
            {
                await _target.SendStreamingUploadChunkEventAsync(READ_STREAM.ToArray(), STORAGE_URI, ACTION_ID, START_POSITION, checkSum, CancellationToken.None);
            });

        }

        private bool CheckMessageContent(Message msg, int chunkSize, string fileName, string actionId, long? startPosition, long? endPosition)
        {
            string messageString = Encoding.UTF8.GetString(msg.GetBytes());
            FirmwareUpdateEvent firmwareUpdateEvent = JsonConvert.DeserializeObject<FirmwareUpdateEvent>(messageString);
            return firmwareUpdateEvent.ChunkSize == chunkSize &&
                  firmwareUpdateEvent.StartPosition == startPosition &&
                  firmwareUpdateEvent.FileName == fileName &&
                  firmwareUpdateEvent.ActionId == actionId &&
                  firmwareUpdateEvent.EndPosition == endPosition;
        }

        private async Task<string> CalculateMdsCheckSumAsync(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                stream.Seek(0, SeekOrigin.Begin);
                byte[] hashBytes = await MD5.Create().ComputeHashAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                string checkSum = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
                return checkSum;
            }
        }

    }
}