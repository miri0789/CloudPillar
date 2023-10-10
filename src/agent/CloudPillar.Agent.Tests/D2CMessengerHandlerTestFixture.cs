using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Moq;
using Microsoft.Azure.Devices.Client;
using System.Text;
using Newtonsoft.Json;
using Shared.Logger;
using Shared.Entities.Messages;
using Shared.Entities.Factories;

namespace CloudPillar.Agent.Tests
{
    [TestFixture]
    public class D2CMessengerHandlerTestFixture
    {

        private Mock<IDeviceClientWrapper> _deviceClientMock;
        private Mock<IMessageFactory> _messageFactory;
        private Mock<ILoggerHandler> _loggerMock;
        private ID2CMessengerHandler _target;

        private const string FILE_NAME = "fileName.txt";
        private const string ACTION_ID = "action123";
        private const long START_POSITION = 10;
        private const long END_POSITION = 20;
        private const int KB = 1024;

        [SetUp]
        public void Setup()
        {
            _deviceClientMock = new Mock<IDeviceClientWrapper>();
            _messageFactory = new Mock<IMessageFactory>();
            _loggerMock = new Mock<ILoggerHandler>();

            _target = new D2CMessengerHandler(_deviceClientMock.Object, _messageFactory.Object, _loggerMock.Object);

        }

        [TestCase(TransportType.Mqtt, 32 * KB)]
        [TestCase(TransportType.Amqp, 64 * KB)]
        [TestCase(TransportType.Http1, 256 * KB)]
        [TestCase((TransportType)100, 32 * KB)] // Unknown transport type
        public async Task SendFirmwareUpdateEventAsync_ByTransportType_SendCorrectChunkSize(TransportType transportType, int expectedChunkSize)
        {
            _deviceClientMock.Setup(dc => dc.GetTransportType()).Returns(transportType);

            await _target.SendFirmwareUpdateEventAsync(FILE_NAME, ACTION_ID, START_POSITION, END_POSITION);
            _deviceClientMock.Verify(dc => dc.SendEventAsync(It.Is<Message>(msg => CheckMessageContent(msg, expectedChunkSize, FILE_NAME, ACTION_ID, START_POSITION, END_POSITION) == true)), Times.Once);
        }



        [Test]
        public async Task SendFirmwareUpdateEventAsync_Failure_ThrowException()
        {
            _deviceClientMock.Setup(dc => dc.GetTransportType()).Returns(TransportType.Mqtt);

            _deviceClientMock.Setup(dc => dc.SendEventAsync(It.IsAny<Message>())).ThrowsAsync(new Exception());

            Assert.ThrowsAsync<Exception>(async () =>
            {
                await _target.SendFirmwareUpdateEventAsync(FILE_NAME, ACTION_ID);
            });
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
    }
}