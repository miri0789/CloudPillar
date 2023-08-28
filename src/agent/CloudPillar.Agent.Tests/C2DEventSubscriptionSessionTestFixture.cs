using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Shared.Entities.Factories;
using Moq;
using Microsoft.Azure.Devices.Client;
using CloudPillar.Agent.Entities;
using Shared.Entities.Messages;

[TestFixture]
public class C2DEventSubscriptionSessionTestFixture
{

    private Mock<IDeviceClientWrapper> _deviceClientMock;
    private Mock<IMessageSubscriber> _messageSubscriberMock;
    private Mock<IMessageFactory> _messageFactoryMock;
    private Mock<ITwinHandler> _twinHandlerMock;
    private IC2DEventSubscriptionSession _target;

    private const string MESSAGE_TYPE_PROP = "MessageType";
        private DownloadBlobChunkMessage _downloadBlobChunkMessage = new DownloadBlobChunkMessage() { MessageType = MessageType.DownloadChunk };


    [SetUp]
    public void Setup()
    {
        _deviceClientMock = new Mock<IDeviceClientWrapper>();
        _messageSubscriberMock = new Mock<IMessageSubscriber>();
        _messageFactoryMock = new Mock<IMessageFactory>();
        _twinHandlerMock = new Mock<ITwinHandler>();


        _target = new C2DEventSubscriptionSession(
             _deviceClientMock.Object,
             _messageSubscriberMock.Object,
             _messageFactoryMock.Object,
             _twinHandlerMock.Object);

             
        var receivedMessage = new Message();
        receivedMessage.Properties[MESSAGE_TYPE_PROP] = MessageType.DownloadChunk.ToString();

        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(receivedMessage);

        _messageFactoryMock
            .Setup(mf => mf.CreateBaseMessageFromMessage<DownloadBlobChunkMessage>(It.IsAny<Message>()))
            .Returns(_downloadBlobChunkMessage);

        var actionToReport = new ActionToReport();
        _messageSubscriberMock
            .Setup(ms => ms.HandleDownloadMessageAsync(_downloadBlobChunkMessage))
            .ReturnsAsync(actionToReport);

        _twinHandlerMock.Setup(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>())).Returns(Task.CompletedTask);

    }

    [Test, Order(1)]
    public async Task ReceiveC2DMessagesAsync_ValidDownloadMessage_CallDownloadHandler()
    {
        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());
        _messageSubscriberMock.Verify(ms => ms.HandleDownloadMessageAsync(_downloadBlobChunkMessage), Times.AtLeastOnce);
    }

    [Test, Order(2)]
    public async Task ReceiveC2DMessagesAsync_ValidDownloadMessage_CallReportHandler()
    {
        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _twinHandlerMock.Verify(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>()), Times.AtLeastOnce);
    }

    [Test, Order(3)]
    public async Task ReceiveC2DMessagesAsync_UnknownMessageType_NotReportCompleteMsg()
    {
        var receivedMessage = new Message();
        receivedMessage.Properties[MESSAGE_TYPE_PROP] = "Try";
        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(receivedMessage);

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _twinHandlerMock.Verify(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>()), Times.Never);
    }

    [Test, Order(3)]
    public async Task ReceiveC2DMessagesAsync_UnknownMessageType_CompleteMsg()
    {
        var receivedMessage = new Message();
        receivedMessage.Properties[MESSAGE_TYPE_PROP] = "Try";
        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(receivedMessage);

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _deviceClientMock.Verify(dc => dc.CompleteAsync(receivedMessage), Times.AtLeastOnce);
    }


    [Test, Order(4)]
    public async Task ReceiveC2DMessagesAsync_ReceivingException_IgnoreMessage()
    {
        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _deviceClientMock.Verify(dc => dc.CompleteAsync(It.IsAny<Message>()), Times.Never);
    }

    [Test, Order(5)]
    public async Task ReceiveC2DMessagesAsync_DownloadingException_CompleteMessage()
    {
        var receivedMessage = new Message();
        receivedMessage.Properties[MESSAGE_TYPE_PROP] = MessageType.DownloadChunk.ToString();

        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(receivedMessage);
        _messageFactoryMock
            .Setup(mf => mf.CreateBaseMessageFromMessage<DownloadBlobChunkMessage>(It.IsAny<Message>()))
            .Returns(_downloadBlobChunkMessage);

        _messageSubscriberMock
            .Setup(ms => ms.HandleDownloadMessageAsync(_downloadBlobChunkMessage))
            .ThrowsAsync(new Exception());

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _deviceClientMock.Verify(dc => dc.CompleteAsync(receivedMessage), Times.AtLeastOnce);
    }

    private CancellationToken GetCancellationToken()
    {
        var cts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMilliseconds(1);
        cts.CancelAfter(timeout);
        return cts.Token;
    }
}