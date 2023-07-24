using common;
using Backend.Iotlistener.Services;
using Backend.Iotlistener.Interfaces;
using Moq;
using Shared.Entities.Events;
using Shared.Logger;

namespace Backend.Iotlistener.Tests;
public class SigningTestFixture
{
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private Mock<ILoggerHandler> _mockLoggerHandler;
    private SigningService _signingService;
    private Uri _signingUrl;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;

    const string deviceId = "testDeviceId";
    const string keyPath = "testKeyPath";
    const string signatureKey = "testSignatureKey";

    [SetUp]
    public void Setup()
    {
        _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
        _mockLoggerHandler = new Mock<ILoggerHandler>();
        _signingUrl = new Uri("http://example.com/");
        _mockEnvironmentsWrapper.Setup(f => f.signingUrl).Returns(_signingUrl.AbsoluteUri);
        _httpRequestorServiceMock = new Mock<IHttpRequestorService>();
        _signingService = new SigningService(_httpRequestorServiceMock.Object, _mockEnvironmentsWrapper.Object, _mockLoggerHandler.Object);
    }

    [Test]
    public async Task CreateTwinKeySignature_ValidParameters_SendsRequest()
    {
        SignEvent signEvent = new SignEvent
        {
            KeyPath = keyPath,
            SignatureKey = signatureKey
        };

        string requestUrl = $"{_signingUrl.AbsoluteUri}signing/create?deviceId={deviceId}&keyPath={signEvent.KeyPath}&signatureKey={signEvent.SignatureKey}";

        await _signingService.CreateTwinKeySignature(deviceId, signEvent);

        _httpRequestorServiceMock.Verify(
            service => service.SendRequest(requestUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public void CreateTwinKeySignature_ExceptionThrown_WrapsAndThrowsException()
    {
        SignEvent signEvent = new SignEvent
        {
            KeyPath = keyPath,
            SignatureKey = signatureKey
        };

        string requestUrl = $"{_signingUrl.AbsoluteUri}signing/create?deviceId={deviceId}&keyPath={signEvent.KeyPath}&signatureKey={signEvent.SignatureKey}";

        _httpRequestorServiceMock
            .Setup(service => service.SendRequest(requestUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Throws<Exception>();

        _httpRequestorServiceMock.Verify(
            service => service.SendRequest(requestUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
