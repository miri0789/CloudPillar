using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Factories;
using Moq;
using Microsoft.Azure.Devices.Client;
using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using CloudPillar.Agent.Handlers.Logger;
using Newtonsoft.Json.Linq;
using CloudPillar.Agent.Utilities.Interfaces;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class C2DEventSubscriptionSessionTestFixture
{

    private Mock<IDeviceClientWrapper> _deviceClientMock;
    private Mock<IMessageSubscriber> _messageSubscriberMock;
    private Mock<IMessageFactory> _messageFactoryMock;
    private Mock<ICheckExceptionResult> _checkExceptionResultMock;
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IStateMachineHandler> _stateMachineHandlerMock;
    private IC2DEventSubscriptionSession _target;
    private const string MESSAGE_TYPE_PROP = "MessageType";
    private DownloadBlobChunkMessage _downloadBlobChunkMessage = new DownloadBlobChunkMessage() { MessageType = C2DMessageType.DownloadChunk };
    private RequestDeviceCertificateMessage _requestDeviceCertificateMessage = new RequestDeviceCertificateMessage() { MessageType = C2DMessageType.RequestDeviceCertificate };
    private ReprovisioningMessage _reprovisioningMessage = new ReprovisioningMessage() { MessageType = C2DMessageType.Reprovisioning };

    [SetUp]
    public void Setup()
    {
        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _messageSubscriberMock = new Mock<IMessageSubscriber>();
        _messageFactoryMock = new Mock<IMessageFactory>();
        _checkExceptionResultMock = new Mock<ICheckExceptionResult>();
        _loggerMock = new Mock<ILoggerHandler>();
        _stateMachineHandlerMock = new Mock<IStateMachineHandler>();

        _target = new C2DEventSubscriptionSession(
             _deviceClientMock.Object,
             _messageSubscriberMock.Object,
             _messageFactoryMock.Object,
             _stateMachineHandlerMock.Object,
             _checkExceptionResultMock.Object,
             _loggerMock.Object);



        var receivedMessage = SetRecivedMessageWithDurationMock(C2DMessageType.DownloadChunk.ToString());

        _messageFactoryMock
            .Setup(mf => mf.CreateC2DMessageFromMessage<DownloadBlobChunkMessage>(It.IsAny<Message>()))
            .Returns(_downloadBlobChunkMessage);

        var actionToReport = new ActionToReport();
        _messageSubscriberMock
            .Setup(ms => ms.HandleDownloadMessageAsync(_downloadBlobChunkMessage, GetCancellationToken()));

    }

    [Test]
    public async Task ReceiveC2DMessagesAsync_ValidDownloadMessage_CallDownloadHandler()
    {
        await _target.ReceiveC2DMessagesAsync(GetCancellationToken(), false);
        _messageSubscriberMock.Verify(ms => ms.HandleDownloadMessageAsync(_downloadBlobChunkMessage, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ReceiveC2DMessagesAsync_ValidRequestDeviceCertificateMessage_CalleRequestDeviceCertificateHandler()
    {
        var receivedMessage = SetRecivedMessageWithDurationMock(C2DMessageType.RequestDeviceCertificate.ToString());

        _messageFactoryMock
            .Setup(mf => mf.CreateC2DMessageFromMessage<RequestDeviceCertificateMessage>(It.IsAny<Message>()))
            .Returns(_requestDeviceCertificateMessage);

        _messageSubscriberMock
       .Setup(ms => ms.HandleRequestDeviceCertificateAsync(_requestDeviceCertificateMessage, It.IsAny<CancellationToken>()));

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken(), true);

        _messageSubscriberMock.Verify(ms => ms.HandleRequestDeviceCertificateAsync(It.IsAny<RequestDeviceCertificateMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ReceiveC2DMessagesAsync_ValidReprovisioningMessage_CallReprovisioningHandler()
    {
        var receivedMessage = SetRecivedMessageWithDurationMock(C2DMessageType.Reprovisioning.ToString());

        _messageFactoryMock
            .Setup(mf => mf.CreateC2DMessageFromMessage<ReprovisioningMessage>(It.IsAny<Message>()))
            .Returns(_reprovisioningMessage);

        _messageSubscriberMock
       .Setup(ms => ms.HandleReprovisioningMessageAsync(receivedMessage, _reprovisioningMessage, It.IsAny<CancellationToken>()));

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken(), true);

        _messageSubscriberMock.Verify(ms => ms.HandleReprovisioningMessageAsync(receivedMessage, It.IsAny<ReprovisioningMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Test]
    public async Task ReceiveC2DMessagesAsync_UnknownMessageType_CompleteMsg()
    {
        var receivedMessage = SetRecivedMessageWithDurationMock("Try");

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken(), false);

        _deviceClientMock.Verify(dc => dc.CompleteAsync(receivedMessage, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Test]
    public async Task ReceiveC2DMessagesAsync_ReceivingException_IgnoreMessage()
    {
        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken(), false);

        _deviceClientMock.Verify(dc => dc.CompleteAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ReceiveC2DMessagesAsync_DownloadingException_CompleteMessage()
    {
        var receivedMessage = SetRecivedMessageWithDurationMock(C2DMessageType.DownloadChunk.ToString());
        _messageFactoryMock
            .Setup(mf => mf.CreateC2DMessageFromMessage<DownloadBlobChunkMessage>(It.IsAny<Message>()))
            .Returns(_downloadBlobChunkMessage);

        _messageSubscriberMock
            .Setup(ms => ms.HandleDownloadMessageAsync(_downloadBlobChunkMessage, GetCancellationToken()))
            .ThrowsAsync(new Exception());

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken(), false);

        _deviceClientMock.Verify(dc => dc.CompleteAsync(receivedMessage, It.IsAny<CancellationToken>()), Times.Once);
    }

    private CancellationToken GetCancellationToken()
    {
        var cts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMilliseconds(100);
        cts.CancelAfter(timeout);
        return cts.Token;
    }

    private Message SetRecivedMessageWithDurationMock(string messageType)
    {
        var receivedMessage = new Message();
        receivedMessage.Properties[MESSAGE_TYPE_PROP] = messageType;

        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).Returns(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
                return receivedMessage;
            });

        return receivedMessage;
    }

}