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

    private Mock<IDeviceClientWrapper> _dviceClientMock;
    private Mock<IMessageSubscriber> _messageSubscriberMock;
    private Mock<IMessagesFactory> _mssagesFactoryMock;
    private Mock<ITwinHandler> _twinHandlerMock;
    private IC2DEventSubscriptionSession _c2DEventSubscriptionSession;

    private const string messageTypeProp = "MessageType";


    [SetUp]
    public void Setup()
    {
        _dviceClientMock = new Mock<IDeviceClientWrapper>();
        _messageSubscriberMock = new Mock<IMessageSubscriber>();
        _mssagesFactoryMock = new Mock<IMessagesFactory>();
        _twinHandlerMock = new Mock<ITwinHandler>();


        _c2DEventSubscriptionSession = new C2DEventSubscriptionSession(
             _dviceClientMock.Object,
             _messageSubscriberMock.Object,
             _mssagesFactoryMock.Object,
             _twinHandlerMock.Object);

    }

    [Test, Order(1)]
    public async Task ReceiveC2DMessagesAsync_ValidDownloadMessage_CallDownloadHandler()
    {
        var receivedMessage = new Message();
        receivedMessage.Properties[messageTypeProp] = MessageType.DownloadChunk.ToString();

        _dviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(receivedMessage);

        var downloadBlobChunkMessage = new DownloadBlobChunkMessage() { MessageType = MessageType.DownloadChunk };
        _mssagesFactoryMock
            .Setup(mf => mf.CreateBaseMessageFromMessage<DownloadBlobChunkMessage>(It.IsAny<Message>()))
            .Returns(downloadBlobChunkMessage);

        var actionToReport = new ActionToReport();
        _messageSubscriberMock
            .Setup(ms => ms.HandleDownloadMessageAsync(downloadBlobChunkMessage))
            .ReturnsAsync(actionToReport);

        _twinHandlerMock.Setup(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>())).Returns(Task.CompletedTask);

        await _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(GetCancellationToken());
        _dviceClientMock.Verify(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _mssagesFactoryMock.Verify(mf => mf.CreateBaseMessageFromMessage<DownloadBlobChunkMessage>(receivedMessage), Times.Once);
        _messageSubscriberMock.Verify(ms => ms.HandleDownloadMessageAsync(downloadBlobChunkMessage), Times.Once);
        _twinHandlerMock.Verify(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>()), Times.Once);
        _dviceClientMock.Verify(dc => dc.CompleteAsync(receivedMessage), Times.AtLeastOnce);
    }

    [Test, Order(2)]
    public async Task ReceiveC2DMessagesAsync_UnknownMessageType_CompleteMsg()
    {
        var receivedMessage = new Message();
        receivedMessage.Properties[messageTypeProp] = "Try";
        _dviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(receivedMessage);

        await _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(GetCancellationToken());

        _dviceClientMock.Verify(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _twinHandlerMock.Verify(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>()), Times.Never);
        _dviceClientMock.Verify(dc => dc.CompleteAsync(receivedMessage), Times.AtLeastOnce);
    }


    [Test, Order(3)]
    public async Task ReceiveC2DMessagesAsync_ReceivingException_IgnoreMessage()
    {
        _dviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception());

        await _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(GetCancellationToken());

        _dviceClientMock.Verify(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _dviceClientMock.Verify(dc => dc.CompleteAsync(It.IsAny<Message>()), Times.Never);
    }

    [Test, Order(4)]
    public async Task ReceiveC2DMessagesAsync_DownloadingException_CompleteMessage()
    {
         var receivedMessage = new Message();
        receivedMessage.Properties[messageTypeProp] = MessageType.DownloadChunk.ToString();

        _dviceClientMock.Setup(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>())).ReturnsAsync(receivedMessage);

        var downloadBlobChunkMessage = new DownloadBlobChunkMessage() { MessageType = MessageType.DownloadChunk };
        _mssagesFactoryMock
            .Setup(mf => mf.CreateBaseMessageFromMessage<DownloadBlobChunkMessage>(It.IsAny<Message>()))
            .Returns(downloadBlobChunkMessage);

        _messageSubscriberMock
            .Setup(ms => ms.HandleDownloadMessageAsync(downloadBlobChunkMessage))
            .ThrowsAsync(new Exception());

        await _c2DEventSubscriptionSession.ReceiveC2DMessagesAsync(GetCancellationToken());

        _dviceClientMock.Verify(dc => dc.ReceiveAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _messageSubscriberMock.Verify(ms => ms.HandleDownloadMessageAsync(downloadBlobChunkMessage), Times.AtLeastOnce);
        _twinHandlerMock.Verify(th => th.UpdateReportActionAsync(It.IsAny<IEnumerable<ActionToReport>>()), Times.Never);
        _dviceClientMock.Verify(dc => dc.CompleteAsync(receivedMessage), Times.AtLeastOnce);
    }

    private CancellationToken GetCancellationToken()
    {
        var cts = new CancellationTokenSource();
        var timeout = TimeSpan.FromMilliseconds(1);
        cts.CancelAfter(timeout);
        return cts.Token;
    }
}