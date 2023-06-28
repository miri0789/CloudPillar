using common;
using Moq;
using shared.Entities;

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
            KeyPath = "testKeyPath",
            SignatureKey = "testSignatureKey"
        };

        string requestUrl = $"{signingUrl.AbsoluteUri}signing/create?deviceId={deviceId}&keyPath={signEvent.KeyPath}&signatureKey={signEvent.SignatureKey}";

        await _signingService.CreateTwinKeySignature(deviceId, signEvent);

        _httpRequestorServiceMock.Verify(
            service => service.SendRequest(requestUrl, HttpMethod.Post, It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public void CreateTwinKeySignature_ExceptionThrown_WrapsAndRethrowsException()
    {
        string deviceId = "testDeviceId";
        SignEvent signEvent = new SignEvent
        {
            KeyPath = "testKeyPath",
            SignatureKey = "testSignatureKey"
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
