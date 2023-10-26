using Backend.Infra.Common;
using Backend.Iotlistener.Services;
using Backend.Iotlistener.Interfaces;
using Moq;
using Shared.Entities.Messages;
using Shared.Logger;

namespace Backend.Iotlistener.Tests;
public class SigningTestFixture
{
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private Mock<ILoggerHandler> _mockLoggerHandler;
    private ISigningService _target;
    private Uri _keyHolderUrl;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;

    const string deviceId = "testDeviceId";
    const string keyPath = "testKeyPath";
    const string signatureKey = "testSignatureKey";

    [SetUp]
    public void Setup()
    {
        _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
        _mockLoggerHandler = new Mock<ILoggerHandler>();
        _keyHolderUrl = new Uri("http://example.com/");
        _mockEnvironmentsWrapper.Setup(f => f.keyHolderUrl).Returns(_keyHolderUrl.AbsoluteUri);
        _httpRequestorServiceMock = new Mock<IHttpRequestorService>();
        _target = new SigningService(_httpRequestorServiceMock.Object, _mockEnvironmentsWrapper.Object, _mockLoggerHandler.Object);
    }

    [Test]
    public async Task CreateTwinKeySignature_ValidParameters_SendsRequest()
    {
        SignEvent signEvent = new SignEvent
        {
            KeyPath = keyPath,
            SignatureKey = signatureKey
        };

        string requestUrl = $"{_keyHolderUrl.AbsoluteUri}signing/create?deviceId={deviceId}&keyPath={signEvent.KeyPath}&signatureKey={signEvent.SignatureKey}";

        await _target.CreateTwinKeySignature(deviceId, signEvent);

        _httpRequestorServiceMock.Verify(
            service => service.SendRequest(requestUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task CreateTwinKeySignature_ExceptionThrown_WrapsAndThrowsException()
    {
        SignEvent signEvent = new SignEvent
        {
            KeyPath = keyPath,
            SignatureKey = signatureKey
        };

        string requestUrl = $"{_keyHolderUrl.AbsoluteUri}signing/create?deviceId={deviceId}&keyPath={signEvent.KeyPath}&signatureKey={signEvent.SignatureKey}";

        _httpRequestorServiceMock
            .Setup(service => service.SendRequest(requestUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        await _target.CreateTwinKeySignature(deviceId, signEvent);

        _httpRequestorServiceMock.Verify(
            service => service.SendRequest(requestUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}
