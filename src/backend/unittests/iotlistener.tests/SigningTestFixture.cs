using common;
using Moq;

namespace iotlistener.tests;
public class SigningTestFixture
{
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private SigningService _signingService;
    private Uri signingUrl;

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
        string deviceId = "testDeviceId";
        SignEvent signEvent = new SignEvent
        {
            keyPath = "testKeyPath",
            signatureKey = "testSignatureKey"
        };

        string requestUrl = $"{signingUrl.AbsoluteUri}signing/create?deviceId={deviceId}&keyPath={signEvent.keyPath}&signatureKey={signEvent.signatureKey}";

        await _signingService.CreateTwinKeySignature(deviceId, signEvent);

        _httpRequestorServiceMock.Verify(
            service => service.SendRequest<object>(requestUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public void CreateTwinKeySignature_ExceptionThrown_WrapsAndRethrowsException()
    {
        string deviceId = "testDeviceId";
        SignEvent signEvent = new SignEvent
        {
            keyPath = "testKeyPath",
            signatureKey = "testSignatureKey"
        };

        string requestUrl = $"{signingUrl.AbsoluteUri}signing/create?deviceId={deviceId}&keyPath={signEvent.keyPath}&signatureKey={signEvent.signatureKey}";

        _httpRequestorServiceMock
            .Setup(service => service.SendRequest<object>(requestUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Throws<Exception>();

        Assert.ThrowsAsync<Exception>(async () =>
        {
            await _signingService.CreateTwinKeySignature(deviceId, signEvent);
        });
    }
}
