using Moq;
using System.Text;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using static Microsoft.Azure.EventHubs.EventData;
using Shared.Entities.Events;
using Backend.Iotlistener.Services;
using Backend.Iotlistener.Interfaces;
using Shared.Logger;

namespace Backend.Iotlistener.Tests;



public class AgentEventProcessorTestFixture
{
    private AgentEventProcessor _eventProcessor;
    private Mock<IFirmwareUpdateService> _firmwareUpdateServiceMock;
    private Mock<ISigningService> _signingServiceMock;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
    private Mock<ILoggerHandler> _mockLoggerHandler;

    private string _iothubConnectionDeviceId;

    [SetUp]
    public void Setup()
    {
        _iothubConnectionDeviceId = "abcd1234";
        _firmwareUpdateServiceMock = new Mock<IFirmwareUpdateService>();
        _signingServiceMock = new Mock<ISigningService>();
        _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
        _mockLoggerHandler = new Mock<ILoggerHandler>();
        _mockEnvironmentsWrapper.Setup(f => f.messageTimeoutMinutes).Returns(30);
        _mockEnvironmentsWrapper.Setup(f => f.iothubConnectionDeviceId).Returns(_iothubConnectionDeviceId);
        _eventProcessor = new AgentEventProcessor(_firmwareUpdateServiceMock.Object,
        _signingServiceMock.Object, _mockEnvironmentsWrapper.Object, _mockLoggerHandler.Object);

    }


    private List<EventData> InitMessage(string body)
    {
        byte[] eventDataBody = Encoding.UTF8.GetBytes(body);
        var eventDataMock = new EventData(eventDataBody);
        eventDataMock.SystemProperties = new SystemPropertiesCollection(0, DateTime.UtcNow.AddMinutes(-10), "0", "1");
        eventDataMock.SystemProperties[_iothubConnectionDeviceId] = "deviceId";
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
    public async Task ProcessEventsAsync_SignTwinKeyMessage_CallSignTwinKey_ThrowError()
    {
        _signingServiceMock
             .Setup(service => service.CreateTwinKeySignature(It.IsAny<string>(), It.IsAny<Shared.Entities.Events.SignEvent>()))
             .ThrowsAsync(new Exception("Failed to create twin key signature"));

        var messages = InitMessage("{\"EventType\": 1, \"KeyPath\": \"keyPath1\",\"SignatureKey\": \"signatureKey\"}");
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _eventProcessor.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Once);
        _mockLoggerHandler.Verify(l => l.Error(
            It.Is<string>(msg => msg.Contains($"Failed parsing message on Partition: 1, Error: Failed to create twin key signature - Ignoring")),
            It.IsAny<object[]>()), Times.Once);
    }

    [Test]
    public async Task ProcessEventsAsync_DrainMode_NotCall()
    {
        _mockEnvironmentsWrapper.Setup(f => f.drainD2cQueues).Returns("drainD2cQueues");
        var messages = InitMessage("{\"EventType\": 1, \"KeyPath\": \"keyPath1\",\"SignatureKey\": \"signatureKey\"}");
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _eventProcessor.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Never);
        _mockLoggerHandler.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains($"Draining on Partition: 1")), It.IsAny<object[]>()), Times.Once);
    }

    [Test]
    public async Task ProcessEventsAsync_ExpiredTimeOutMessage_NotCall()
    {
        var messages = InitMessage("{\"EventType\": 1, \"KeyPath\": \"keyPath1\",\"SignatureKey\": \"signatureKey\"}");
        _mockEnvironmentsWrapper.Setup(f => f.messageTimeoutMinutes).Returns(1);
        _eventProcessor = new AgentEventProcessor(_firmwareUpdateServiceMock.Object, _signingServiceMock.Object, _mockEnvironmentsWrapper.Object, _mockLoggerHandler.Object);
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _eventProcessor.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Never);
        _mockLoggerHandler.Verify(l => l.Warn(It.Is<string>(msg => msg.Contains("Ignoring message older than 1 minutes.")), It.IsAny<object[]>()), Times.Once);
    }

}
