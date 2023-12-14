using System.Security.Cryptography;
using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers.Logger;
using System.Text;


namespace CloudPillar.Agent.Tests;

[TestFixture]
public class SignatureHandlerTestFixture
{
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private Mock<ECDsa> _ecdsaMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
    private ISignatureHandler _target;


    [SetUp]
    public void Setup()
    {
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
        _target = new SignatureHandler(_fileStreamerWrapperMock.Object, _loggerHandlerMock.Object, _d2CMessengerHandlerMock.Object);
        _ecdsaMock = new Mock<ECDsa>();

        string publicKeyPem = @"-----BEGIN PUBLIC KEY-----
                                MIGbMBAGByqGSM49AgEGBSuBBAAjA4GGAAQBgc4HZz+/fBbC7lmEww0AO3NK9wVZ
                                PDZ0VEnsaUFLEYpTzb90nITtJUcPUbvOsdZIZ1Q8fnbquAYgxXL5UgHMoywAib47
                                6MkyyYgPk0BXZq3mq4zImTRNuaU9slj9TVJ3ScT3L1bXwVuPJDzpr5GOFpaj+WwM
                                Al8G7CqwoJOsW7Kddns=
                                -----END PUBLIC KEY-----";
        _fileStreamerWrapperMock.Setup(f => f.ReadAllTextAsync(It.IsAny<string>())).ReturnsAsync(() => publicKeyPem);
    }



    [Test]
    public async Task VerifySignature_ValidSignature_ReturnsTrue()
    {
        string message = "value";
        string signatureString = "AamBQZxGNBGWsm9NkOyWiZRCWGponIRIJo3nnKytRyQlcpJv/iUy5fS1FUodBAX6Sn5kJV9g3DMn2GkJovSWOFXTAdNgY+OJsV42919LetahmaR1M7V8wcHqm+0ddfwF9MzO11fl39PZTT6upInvAVb8KA7Hazjn9enCWCxcise/2RZy";
        bool result = await _target.VerifySignatureAsync(Encoding.UTF8.GetBytes(message), signatureString);
        Assert.IsTrue(result);
    }

    [Test]
    public async Task VerifySignature_InvalidSignature_ReturnsFalse()
    {
        string message = "Hello, world!";
        string signatureString = "SGVsbG8sIHdvcmxkIQ==";
        bool result = await _target.VerifySignatureAsync(Encoding.UTF8.GetBytes(message), signatureString);
        Assert.IsFalse(result);
    }


}