using Moq;
using Shared.Logger;
using Microsoft.Azure.Devices;
using Backend.Infra.Common.Wrappers.Interfaces;
using Backend.Infra.Common.Services;

[TestFixture]
public class DeviceConnectServiceTestFixture
{
    private Mock<ILoggerHandler> _mockLoggerHandler;
    private Mock<IDeviceClientWrapper> _mockDeviceClientWrapper;
    private Mock<ICommonEnvironmentsWrapper> _mockEnvironmentsWrapper;
    private DeviceConnectService _target;


    [SetUp]
    public void SetUp()
    {
        _mockLoggerHandler = new Mock<ILoggerHandler>();
        _mockDeviceClientWrapper = new Mock<IDeviceClientWrapper>();
        _mockEnvironmentsWrapper = new Mock<ICommonEnvironmentsWrapper>();
        _mockEnvironmentsWrapper.Setup(f => f.iothubConnectionString).Returns("123456");
        _mockEnvironmentsWrapper.Setup(f => f.retryPolicyExponent).Returns(3);
        var mockDeviceClient = new Mock<ServiceClient>();
        _mockDeviceClientWrapper.Setup(c => c.CreateFromConnectionString())
        .Returns(mockDeviceClient.Object);
        _target = new DeviceConnectService(_mockLoggerHandler.Object, _mockDeviceClientWrapper.Object, _mockEnvironmentsWrapper.Object);
    }


    [Test]
    public async Task SendMessage_ShouldRetryAndSucceed()
    {
        _mockDeviceClientWrapper
            .SetupSequence(s => s.SendAsync(It.IsAny<ServiceClient>(), It.IsAny<string>(), It.IsAny<Message>()))
            .Throws(new Exception("First send failed"))
            .Throws(new Exception("Second send failed"))
            .Returns(Task.CompletedTask);

        await _target.SendDeviceMessageAsync(new Message(), "");
        _mockDeviceClientWrapper.Verify(s => s.SendAsync(It.IsAny<ServiceClient>(), It.IsAny<string>(), It.IsAny<Message>()), Times.Exactly(3));
    }


}
