using common;
using iotlistener.Services;
using Moq;
using shared.Entities.Events;

namespace iotlistener.tests;
public class SigningTestFixture
{
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private SigningService _signingService;
    private Uri signingUrl;

    const string deviceId = "testDeviceId";
    const string keyPath = "testKeyPath";
    const string signatureKey = "testSignatureKey";

    [SetUp]
    public void Setup()
    {
        signingUrl = new Uri("http://example.com/");
        Environment.SetEnvironmentVariable(Constants.signingUrl, signingUrl.AbsoluteUri);
        _httpRequestorServiceMock = new Mock<IHttpRequestorService>();
        _signingService = new SigningService(_httpRequestorServiceMock.Object);
    }

    [Test]
    public async Task CreateTwinKeySignature_ValidParameters_SendsRequest()
    {
        SignEvent signEvent = new SignEvent
        {
            KeyPath = keyPath,
            SignatureKey = signatureKey
        };

        string requestUrl = $"{signingUrl.AbsoluteUri}signing/create?deviceId={deviceId}&keyPath={signEvent.KeyPath}&signatureKey={signEvent.SignatureKey}";

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

        string requestUrl = $"{signingUrl.AbsoluteUri}signing/create?deviceId={deviceId}&keyPath={signEvent.KeyPath}&signatureKey={signEvent.SignatureKey}";

        _httpRequestorServiceMock
            .Setup(service => service.SendRequest(requestUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Throws<Exception>();

        Assert.ThrowsAsync<Exception>(async () =>
        {
            await _signingService.CreateTwinKeySignature(deviceId, signEvent);
        });
    }
}
