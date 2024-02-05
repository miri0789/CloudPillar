using Moq;
using System.Text;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using static Microsoft.Azure.EventHubs.EventData;
using Backend.Iotlistener.Interfaces;
using Shared.Logger;
using Backend.Iotlistener.Processors;
using Shared.Entities.Messages;

namespace Backend.Iotlistener.Tests;



public class AgentEventProcessorTestFixture
{
    private AgentEventProcessor _target;
    private Mock<IFileDownloadService> _FileDownloadServiceMock;
    private Mock<ISigningService> _signingServiceMock;
    private Mock<IStreamingUploadChunkService> _streamingUploadChunkServiceMock;
    private Mock<IProvisionDeviceService> _provisionDeviceCertificateServiceMock;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;
    private Mock<ILoggerHandler> _mockLoggerHandler;
    private Mock<IStreamingUploadChunkService> _streamingUploadChunkService;

    private string _iothubConnectionDeviceId;

    [SetUp]
    public void Setup()
    {
        _iothubConnectionDeviceId = "abcd1234";
        _FileDownloadServiceMock = new Mock<IFileDownloadService>();
        _signingServiceMock = new Mock<ISigningService>();
        _provisionDeviceCertificateServiceMock = new Mock<IProvisionDeviceService>();
        _streamingUploadChunkServiceMock = new Mock<IStreamingUploadChunkService>();
        _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
        _mockLoggerHandler = new Mock<ILoggerHandler>();
        _streamingUploadChunkService = new Mock<IStreamingUploadChunkService>();
        _mockEnvironmentsWrapper.Setup(f => f.messageTimeoutMinutes).Returns(30);
        _mockEnvironmentsWrapper.Setup(f => f.iothubConnectionDeviceId).Returns(_iothubConnectionDeviceId);
        _target = new AgentEventProcessor(_FileDownloadServiceMock.Object,
        _signingServiceMock.Object,
        _streamingUploadChunkServiceMock.Object,
        _provisionDeviceCertificateServiceMock.Object,
        _mockEnvironmentsWrapper.Object,
        _mockLoggerHandler.Object);

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
    public async Task ProcessEventsAsync_DownloadMessage_CallDownloadUpdate()
    {
        var messages = InitMessage("{\"MessageType\": 0, \"FileName\": \"fileName1\",\"ChunkSize\": 1234, \"ActionIndex\":0}");

        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _target.ProcessEventsAsync(contextMock.Object, messages);

        _FileDownloadServiceMock.Verify(f => f.SendFileDownloadAsync("deviceId", It.IsAny<FileDownloadEvent>()), Times.Once);
    }

    [Test]
    public async Task ProcessEventsAsync_SignTwinKeyMessage_CallSignTwinKey()
    {
        var messages = InitMessage("{\"MessageType\": 1, \"KeyPath\": \"keyPath1\",\"SignatureKey\": \"signatureKey\"}");
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _target.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Once);
    }

    [Test]
    public async Task ProcessEventsAsync_DrainMode_NotCall()
    {
        _mockEnvironmentsWrapper.Setup(f => f.drainD2cQueues).Returns("drainD2cQueues");
        var messages = InitMessage("{\"MessageType\": 1, \"KeyPath\": \"keyPath1\",\"SignatureKey\": \"signatureKey\"}");
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _target.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Never);
    }

    [Test]
    public async Task ProcessEventsAsync_ExpiredTimeOutMessage_NotCall()
    {
        var messages = InitMessage("{\"MessageType\": 1, \"KeyPath\": \"keyPath1\",\"SignatureKey\": \"signatureKey\"}");
        _mockEnvironmentsWrapper.Setup(f => f.messageTimeoutMinutes).Returns(1);
        _target = new AgentEventProcessor(
        _FileDownloadServiceMock.Object,
        _signingServiceMock.Object,
        _streamingUploadChunkServiceMock.Object,
        _provisionDeviceCertificateServiceMock.Object,
        _mockEnvironmentsWrapper.Object,
        _mockLoggerHandler.Object);
        var contextMock = new Mock<PartitionContext>(null, "1", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _target.ProcessEventsAsync(contextMock.Object, messages);

        _signingServiceMock.Verify(f => f.CreateTwinKeySignature("deviceId", It.IsAny<SignEvent>()), Times.Never);
    }

    [Test]
    public async Task ProcessEventsAsync_RemoveDeviceMessage_CallRemoveDevice()
    {
        var messages = InitMessage("{\"MessageType\": 5, \"KeyPath\": \"keyPath1\",\"SignatureKey\": \"removedevice\"}");
        var contextMock = new Mock<PartitionContext>(null, "5", "consumerGroupName", "eventHubPath", null)
        {
            CallBase = true
        };

        await _target.ProcessEventsAsync(contextMock.Object, messages);

        _provisionDeviceCertificateServiceMock.Verify(f => f.RemoveDeviceAsync("deviceId"), Times.Once);
    }
}
