using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Factories;
using Moq;
using Microsoft.Azure.Devices.Client;
using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;
using Shared.Logger;

[TestFixture]
public class C2DEventSubscriptionSessionTestFixture
{

    private Mock<IDeviceClientWrapper> _deviceClientMock;
    private Mock<IMessageSubscriber> _messageSubscriberMock;
    private Mock<IMessageFactory> _messageFactoryMock;
    private Mock<ITwinHandler> _twinHandlerMock;
    private Mock<ILoggerHandler> _loggerMock;
    private IC2DEventSubscriptionSession _target;

    private const string MESSAGE_TYPE_PROP = "MessageType";
    private DownloadBlobChunkMessage _downloadBlobChunkMessage = new DownloadBlobChunkMessage() { MessageType = C2DMessageType.DownloadChunk };


    [SetUp]
    public void Setup()
    {
        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _messageSubscriberMock = new Mock<IMessageSubscriber>();
        _messageFactoryMock = new Mock<IMessageFactory>();
        _twinHandlerMock = new Mock<ITwinHandler>();
        _loggerMock = new Mock<ILoggerHandler>();

        _target = new C2DEventSubscriptionSession(
             _deviceClientMock.Object,
             _messageSubscriberMock.Object,
             _messageFactoryMock.Object,
             _twinHandlerMock.Object,
             _loggerMock.Object);


        
        var receivedMessage = SetRecivedMessageWithDurationMock(C2DMessageType.DownloadChunk.ToString());

        _messageFactoryMock
            .Setup(mf => mf.CreateC2DMessageFromMessage<DownloadBlobChunkMessage>(It.IsAny<Message>()))
            .Returns(_downloadBlobChunkMessage);

        var actionToReport = new ActionToReport();
        _messageSubscriberMock
            .Setup(ms => ms.HandleDownloadMessageAsync(_downloadBlobChunkMessage))
            .ReturnsAsync(actionToReport);

        _twinHandlerMock.Setup(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>())).Returns(Task.CompletedTask);

    }

    [Test]
    public async Task ReceiveC2DMessagesAsync_ValidDownloadMessage_CallDownloadHandler()
    {
        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());
        _messageSubscriberMock.Verify(ms => ms.HandleDownloadMessageAsync(_downloadBlobChunkMessage), Times.Once);
    }

    [Test]
    public async Task ReceiveC2DMessagesAsync_ValidDownloadMessage_CallReportHandler()
    {
        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _twinHandlerMock.Verify(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>()), Times.Once);
    }

    [Test]
    public async Task ReceiveC2DMessagesAsync_UnknownMessageType_NotReportCompleteMsg()
    {
        var receivedMessage = SetRecivedMessageWithDurationMock("Try");

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _twinHandlerMock.Verify(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>()), Times.Never);
    }

    [Test]
    public async Task ReceiveC2DMessagesAsync_UnknownMessageType_CompleteMsg()
    {
        var receivedMessage = SetRecivedMessageWithDurationMock("Try");

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _deviceClientMock.Verify(dc => dc.CompleteAsync(receivedMessage), Times.Once);
    }


    [Test]
    public async Task ReceiveC2DMessagesAsync_ReceivingException_IgnoreMessage()
    {
        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _deviceClientMock.Verify(dc => dc.CompleteAsync(It.IsAny<Message>()), Times.Never);
    }

    [Test]
    public async Task ReceiveC2DMessagesAsync_DownloadingException_CompleteMessage()
    {
        var receivedMessage = SetRecivedMessageWithDurationMock(C2DMessageType.DownloadChunk.ToString());
        _messageFactoryMock
            .Setup(mf => mf.CreateC2DMessageFromMessage<DownloadBlobChunkMessage>(It.IsAny<Message>()))
            .Returns(_downloadBlobChunkMessage);

        _messageSubscriberMock
            .Setup(ms => ms.HandleDownloadMessageAsync(_downloadBlobChunkMessage))
            .ThrowsAsync(new Exception());

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _deviceClientMock.Verify(dc => dc.CompleteAsync(receivedMessage), Times.Once);
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