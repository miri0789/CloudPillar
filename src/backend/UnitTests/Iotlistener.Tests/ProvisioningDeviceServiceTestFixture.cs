
using Backend.Infra.Common.Services.Interfaces;
using Backend.Iotlistener.Interfaces;
using Backend.Iotlistener.Services;
using Moq;
using Shared.Logger;

namespace Backend.Iotlistener.Tests;

[TestFixture]
public class ProvisioningDeviceTestFixture
{

    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private Mock<IEnvironmentsWrapper> _environmentsWrapperMock;
    private Mock<ILoggerHandler> _loggerMock;
    private ProvisionDeviceService _target;
    private const string _deviceId = "testDevice";

    [SetUp]
    public void Setup()
    {
        _environmentsWrapperMock = new Mock<IEnvironmentsWrapper>();
        _loggerMock = new Mock<ILoggerHandler>();
        _httpRequestorServiceMock = new Mock<IHttpRequestorService>();
        _target = new ProvisionDeviceService(_httpRequestorServiceMock.Object, _environmentsWrapperMock.Object, _loggerMock.Object);

        _httpRequestorServiceMock.Setup(service =>
                        service.SendRequest<bool>(It.IsAny<string>(), HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
    }

    [Test]
    public async Task ProvisionDeviceCertificateAsync_SendRemoveDeviceEvent()
    {
        await _target.RemoveDeviceAsync(_deviceId);
        _httpRequestorServiceMock.Verify(service => service.SendRequest(It.IsAny<string>(), HttpMethod.Delete, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}