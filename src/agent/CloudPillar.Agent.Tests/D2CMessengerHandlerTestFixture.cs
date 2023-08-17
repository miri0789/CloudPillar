using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Moq;
using Microsoft.Azure.Devices.Client;
using System.Text;
using Newtonsoft.Json;

[TestFixture]
public class D2CMessengerHandlerTestFixture
{

    private Mock<IDeviceClientWrapper> _dviceClientMock;
    private ID2CMessengerHandler _d2CMessengerHandler;

    private const string fileName = "fileName.txt";
    private const string actionId = "action123";
    private const long startPosition = 10;
    private const long endPosition = 20;
    private const int KB = 1024;

    [SetUp]
    public void Setup()
    {
        _dviceClientMock = new Mock<IDeviceClientWrapper>();

        _d2CMessengerHandler = new D2CMessengerHandler(_dviceClientMock.Object);

    }

    [TestCase(TransportType.Mqtt, 32 * KB)]
    [TestCase(TransportType.Amqp, 64 * KB)]
    [TestCase(TransportType.Http1, 256 * KB)]
    [TestCase((TransportType)100, 32 * KB)] // Unknown transport type
    public async Task SendFirmwareUpdateEventAsync_ByTransportType_SendCorrectChunkSize(TransportType transportType, int expectedChunkSize)
    {
        _dviceClientMock.Setup(dc => dc.GetTransportType()).Returns(transportType);

        await _d2CMessengerHandler.SendFirmwareUpdateEventAsync(fileName, actionId, startPosition, endPosition);
        _dviceClientMock.Verify(dc => dc.SendEventAsync(It.Is<Message>(msg => CheckMessageContent(msg, expectedChunkSize, fileName, actionId, startPosition, endPosition) == true)), Times.Once);
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

    [Test]
    public async Task SendFirmwareUpdateEventAsync_Failure_ThrowException()
    {
        _dviceClientMock.Setup(dc => dc.GetTransportType()).Returns(TransportType.Mqtt);

        _dviceClientMock.Setup(dc => dc.SendEventAsync(It.IsAny<Message>())).ThrowsAsync(new Exception());

        Assert.ThrowsAsync<Exception>(async () =>
        {
            await _d2CMessengerHandler.SendFirmwareUpdateEventAsync(fileName, actionId);
        });
    }
    

}