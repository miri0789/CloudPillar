﻿using Moq;
using System.Text;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using static Microsoft.Azure.EventHubs.EventData;

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
        _eventProcessor = new AgentEventProcessor(_firmwareUpdateServiceMock.Object, _signingServiceMock.Object);
    }


    private List<EventData> initMessage(string body)
    {
        var iothubConnectionDeviceId = "abcd1234";
        Environment.SetEnvironmentVariable(Constants.messageTimeoutMinutes, "30");
        Environment.SetEnvironmentVariable(Constants.iothubConnectionDeviceId, iothubConnectionDeviceId);
        byte[] eventDataBody = Encoding.UTF8.GetBytes(body);
        var eventDataMock = new EventData(eventDataBody);
        eventDataMock.SystemProperties = new SystemPropertiesCollection(0, DateTime.UtcNow, "0", "1");
        eventDataMock.SystemProperties[iothubConnectionDeviceId] = "deviceId";
        var messages = new List<EventData> { eventDataMock };
        return messages;
    }

    [Test]
    public async Task ProcessEventsAsync_WhenCalledFirmwareUpdate_CallFirmwareUpdate()
    {
        var messages = initMessage("{\"eventType\": 0, \"fileName\": \"fileName1\",\"chunkSize\": 1234}");

        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _eventProcessor.ProcessEventsAsync(contextMock.Object, messages);

        _firmwareUpdateServiceMock.Verify(f => f.SendFirmwareUpdateAsync("deviceId", It.IsAny<FirmwareUpdateEvent>()), Times.Once);
    }

    [Test]
    public async Task ProcessEventsAsync_WhenCalledSignTwinKey_CallSignTwinKey()
    {
        var messages = initMessage("{\"eventType\": 1, \"keyPath\": \"keyPath1\",\"signatureKey\": \"signatureKey\"}");
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _eventProcessor.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Once);
    }

    [Test]
    public async Task ProcessEventsAsync_WhenCalledSignTwinKey_DrainMode_NotCallSignTwinKey()
    {
        Environment.SetEnvironmentVariable(Constants.drainD2cQueues, "drainD2cQueues");
        var messages = initMessage("{\"eventType\": 1, \"keyPath\": \"keyPath1\",\"signatureKey\": \"signatureKey\"}");
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _eventProcessor.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Never);
    }
    
    [Test]
    public async Task ProcessEventsAsync_WhenCalledSignTwinKey_ExpiredTimeOut_NotCallSignTwinKey()
    {
        var messages = initMessage("{\"eventType\": 1, \"keyPath\": \"keyPath1\",\"signatureKey\": \"signatureKey\"}");
        Environment.SetEnvironmentVariable(Constants.messageTimeoutMinutes, "0");
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _eventProcessor.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Never);
    }

}
