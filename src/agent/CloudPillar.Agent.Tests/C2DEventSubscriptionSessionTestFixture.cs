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

    }

    private DownloadBlobChunkMessage InitSuccessDownloadMocks()
    {
        var receivedMessage = new Message();
        receivedMessage.Properties[MESSAGE_TYPE_PROP] = MessageType.DownloadChunk.ToString();

        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(receivedMessage);

        var downloadBlobChunkMessage = new DownloadBlobChunkMessage() { MessageType = MessageType.DownloadChunk };
        _messageFactoryMock
            .Setup(mf => mf.CreateBaseMessageFromMessage<DownloadBlobChunkMessage>(It.IsAny<Message>()))
            .Returns(downloadBlobChunkMessage);

        var actionToReport = new ActionToReport();
        _messageSubscriberMock
            .Setup(ms => ms.HandleDownloadMessageAsync(downloadBlobChunkMessage))
            .ReturnsAsync(actionToReport);
        _twinHandlerMock.Setup(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>())).Returns(Task.CompletedTask);

        return downloadBlobChunkMessage;
    }

    [Test, Order(1)]
    public async Task ReceiveC2DMessagesAsync_ValidDownloadMessage_CallDownloadHandler()
    {
        var downloadBlobChunkMessage = InitSuccessDownloadMocks();

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());
        _messageSubscriberMock.Verify(ms => ms.HandleDownloadMessageAsync(downloadBlobChunkMessage), Times.Once);
    }

    [Test, Order(2)]
    public async Task ReceiveC2DMessagesAsync_ValidDownloadMessage_CallReportHandler()
    {
        var downloadBlobChunkMessage = InitSuccessDownloadMocks();

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _twinHandlerMock.Verify(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>()), Times.Once);
    }

    [Test, Order(3)]
    public async Task ReceiveC2DMessagesAsync_UnknownMessageType_CompleteMsg()
    {
        var receivedMessage = new Message();
        receivedMessage.Properties[MESSAGE_TYPE_PROP] = "Try";
        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(receivedMessage);

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _deviceClientMock.Verify(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _twinHandlerMock.Verify(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>()), Times.Never);
        _deviceClientMock.Verify(dc => dc.CompleteAsync(receivedMessage), Times.AtLeastOnce);
    }


    [Test, Order(4)]
    public async Task ReceiveC2DMessagesAsync_ReceivingException_IgnoreMessage()
    {
        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _deviceClientMock.Verify(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _deviceClientMock.Verify(dc => dc.CompleteAsync(It.IsAny<Message>()), Times.Never);
    }

    [Test, Order(5)]
    public async Task ReceiveC2DMessagesAsync_DownloadingException_CompleteMessage()
    {
        var receivedMessage = new Message();
        receivedMessage.Properties[MESSAGE_TYPE_PROP] = MessageType.DownloadChunk.ToString();

        _deviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(receivedMessage);

        var downloadBlobChunkMessage = new DownloadBlobChunkMessage() { MessageType = MessageType.DownloadChunk };
        _messageFactoryMock
            .Setup(mf => mf.CreateBaseMessageFromMessage<DownloadBlobChunkMessage>(It.IsAny<Message>()))
            .Returns(downloadBlobChunkMessage);

        _messageSubscriberMock
            .Setup(ms => ms.HandleDownloadMessageAsync(downloadBlobChunkMessage))
            .ThrowsAsync(new Exception());

        await _target.ReceiveC2DMessagesAsync(GetCancellationToken());

        _deviceClientMock.Verify(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _messageSubscriberMock.Verify(ms => ms.HandleDownloadMessageAsync(downloadBlobChunkMessage), Times.AtLeastOnce);
        _twinHandlerMock.Verify(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>()), Times.Never);
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