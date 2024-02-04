using Backend.Infra.Common.Services.Interfaces;
using Backend.Iotlistener.Services;
using Backend.Iotlistener.Interfaces;
using Moq;
using Shared.Entities.Messages;
using Shared.Logger;
using Shared.Entities.Utilities;

namespace Backend.Iotlistener.Tests;
public class SigningTestFixture
{
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private Mock<ILoggerHandler> _mockLoggerHandler;
    private ISigningService _target;
    private Uri _beApiUrl;
    private Mock<IEnvironmentsWrapper> _mockEnvironmentsWrapper;

    const string deviceId = "testDeviceId";
    string changeSignKey;

    [SetUp]
    public void Setup()
    {
        changeSignKey = TwinConstants.CHANGE_SPEC_NAME.GetSignKeyByChangeSpec();
        _mockEnvironmentsWrapper = new Mock<IEnvironmentsWrapper>();
        _mockLoggerHandler = new Mock<ILoggerHandler>();
        _beApiUrl = new Uri("http://example.com/");
        _mockEnvironmentsWrapper.Setup(f => f.keyHolderUrl).Returns(_beApiUrl.AbsoluteUri);
        _httpRequestorServiceMock = new Mock<IHttpRequestorService>();
        _target = new SigningService(_httpRequestorServiceMock.Object, _mockEnvironmentsWrapper.Object, _mockLoggerHandler.Object);
    }

    [Test]
    public async Task CreateTwinKeySignature_ValidParameters_SendsRequest()
    {

        SignEvent signEvent = new SignEvent(changeSignKey);

        string requestUrl = $"ChangeSpec/CreateChangeSpecKeySignature?deviceId={deviceId}&changeSignKey={signEvent.ChangeSignKey}";
        await _target.CreateTwinKeySignature(deviceId, signEvent);

        _httpRequestorServiceMock.Verify(
            service => service.SendRequest(requestUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task CreateTwinKeySignature_ExceptionThrown_WrapsAndThrowsException()
    {
        SignEvent signEvent = new SignEvent(changeSignKey);

        string requestUrl = $"ChangeSpec/CreateChangeSpecKeySignature?deviceId={deviceId}&changeSignKey={signEvent.ChangeSignKey}";

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
