using Moq;
using System.Text;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using static Microsoft.Azure.EventHubs.EventData;
using shared.Entities.Events;
using iotlistener.Services;
using iotlistener.Interfaces;

namespace iotlistener.tests;



public class AgentEventProcessorTestFixture
{
    private AgentEventProcessor _eventProcessor;
    private Mock<IFirmwareUpdateService> _firmwareUpdateServiceMock;
    private Mock<ISigningService> _signingServiceMock;

    [SetUp]
    public void Setup()
    {
        _firmwareUpdateServiceMock = new Mock<IFirmwareUpdateService>();
        _signingServiceMock = new Mock<ISigningService>();
         Environment.SetEnvironmentVariable(Constants.messageTimeoutMinutes, "20");
        _eventProcessor = new AgentEventProcessor(_firmwareUpdateServiceMock.Object, _signingServiceMock.Object);
        Environment.SetEnvironmentVariable(Constants.drainD2cQueues, "");
    }


    private List<EventData> InitMessage(string body)
    {
        var iothubConnectionDeviceId = "abcd1234";
        Environment.SetEnvironmentVariable(Constants.iothubConnectionDeviceId, iothubConnectionDeviceId);
        byte[] eventDataBody = Encoding.UTF8.GetBytes(body);
        var eventDataMock = new EventData(eventDataBody);
        eventDataMock.SystemProperties = new SystemPropertiesCollection(0, DateTime.UtcNow.AddMinutes(-10), "0", "1");
        eventDataMock.SystemProperties[iothubConnectionDeviceId] = "deviceId";
        var messages = new List<EventData> { eventDataMock };
        return messages;
    }

    [Test]
    public async Task ProcessEventsAsync_FirmwareUpdateMessage_CallFirmwareUpdate()
    {
        var messages = InitMessage("{\"EventType\": 0, \"FileName\": \"fileName1\",\"ChunkSize\": 1234, \"ActionGuid\": \"" + new Guid() + "\"}");

        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _eventProcessor.ProcessEventsAsync(contextMock.Object, messages);

        _firmwareUpdateServiceMock.Verify(f => f.SendFirmwareUpdateAsync("deviceId", It.IsAny<FirmwareUpdateEvent>()), Times.Once);
    }

    [Test]
    public async Task ProcessEventsAsync_SignTwinKeyMessage_CallSignTwinKey()
    {
        var messages = InitMessage("{\"EventType\": 1, \"KeyPath\": \"keyPath1\",\"SignatureKey\": \"signatureKey\"}");
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _eventProcessor.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Once);
    }

    [Test]
    public async Task ProcessEventsAsync_DrainMode_NotCall()
    {
        Environment.SetEnvironmentVariable(Constants.drainD2cQueues, "drainD2cQueues");
        var messages = InitMessage("{\"EventType\": 1, \"KeyPath\": \"keyPath1\",\"SignatureKey\": \"signatureKey\"}");
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _eventProcessor.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Never);
    }

    [Test]
    public async Task ProcessEventsAsync_ExpiredTimeOutMessage_NotCall()
    {
        var messages = InitMessage("{\"EventType\": 1, \"KeyPath\": \"keyPath1\",\"SignatureKey\": \"signatureKey\"}");
        Environment.SetEnvironmentVariable(Constants.messageTimeoutMinutes, "1");
        _eventProcessor = new AgentEventProcessor(_firmwareUpdateServiceMock.Object, _signingServiceMock.Object);
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _eventProcessor.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Never);
    }

}
