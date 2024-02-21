
using Azure.Messaging.ServiceBus;
using Backend.Infra.Common.Services;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.Infra.Wrappers.Interfaces;
using Moq;
using Shared.Logger;

[TestFixture]
public class SendQueueMessagesTestFixture
{
    private Mock<ILoggerHandler> _mockLoggerHandler;
    private Mock<ICommonEnvironmentsWrapper> _mockEnvironmentsWrapper;
    private Mock<IServiceBusWrapper> _mockServiceBusWrapper;
    private SendQueueMessagesService _target;

    [SetUp]
    public void SetUp()
    {
        _mockLoggerHandler = new Mock<ILoggerHandler>();
        _mockEnvironmentsWrapper = new Mock<ICommonEnvironmentsWrapper>();
        _mockServiceBusWrapper = new Mock<IServiceBusWrapper>();
        _target = new SendQueueMessagesService(_mockEnvironmentsWrapper.Object, _mockServiceBusWrapper.Object, _mockLoggerHandler.Object);
    }

    [Test]
    public async Task SendMessageToQueue_OnCall_SendMessageToQueue()
    {
        var url = "test-url";
        var message = new object();
        _mockEnvironmentsWrapper.Setup(x => x.serviceBusConnectionString).Returns("test-connection-string");
        _mockEnvironmentsWrapper.Setup(x => x.queueName).Returns("test-queue-name");
        await _target.SendMessageToQueue(url, message);
        _mockServiceBusWrapper.Verify(x => x.SendMessageToQueue(It.IsAny<ServiceBusSender>(), It.IsAny<ServiceBusMessage>()), Times.Once);
    }
}