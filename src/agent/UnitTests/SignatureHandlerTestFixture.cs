using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using System.Text;
using System.Security.Cryptography;
using CloudPillar.Agent.Entities;


namespace CloudPillar.Agent.Tests;

[TestFixture]
public class SignatureHandlerTestFixture
{
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
    private ISignatureHandler _target;
    private Mock<FileStream> _fileStreamMock;
    private Mock<ISHA256Wrapper> _sha256WrapperMock;
    private Mock<IECDsaWrapper> _ecdsaWrapperMock;
    private DownloadSettings mockDownloadSettingsValue = new DownloadSettings();
    private Mock<IOptions<DownloadSettings>> mockDownloadSettings;

    [SetUp]
    public void Setup()
    {
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
        _sha256WrapperMock = new Mock<ISHA256Wrapper>();
        _ecdsaWrapperMock = new Mock<IECDsaWrapper>();
        _fileStreamMock = new Mock<FileStream>(MockBehavior.Default, new object[] { "filePath", FileMode.Create });
        mockDownloadSettingsValue = DownloadSettingsHelper.SetDownloadSettingsValueMock();
        mockDownloadSettings = new Mock<IOptions<DownloadSettings>>();
        mockDownloadSettings.Setup(x => x.Value).Returns(mockDownloadSettingsValue);

        _target = new SignatureHandler(_fileStreamerWrapperMock.Object, _loggerHandlerMock.Object, _d2CMessengerHandlerMock.Object, _sha256WrapperMock.Object, _ecdsaWrapperMock.Object, mockDownloadSettings.Object);

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
        _ecdsaWrapperMock.Setup(f => f.VerifyData(It.IsAny<ECDsa>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<HashAlgorithmName>())).Returns(true);
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

    [TestCase(true)]
    [TestCase(false)]
    public async Task VerifyFileSignatureAsync_ValidateSignature_ReturnsTrue(bool expectedResult)
    {
        string filePath = "pathfile.txt";
        string validSignature = "SGVsbG8sIHdvcmxkIQ==";
        int callCount = 0;
        _fileStreamerWrapperMock.Setup(f => f.Read(It.IsAny<FileStream>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
        .Callback(() => callCount++).Returns(() => callCount == 1 ? 1 : 0);
        _sha256WrapperMock.Setup(f => f.TransformBlock(It.IsAny<SHA256>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<byte[]>(), It.IsAny<int>())).Returns(0);
        _sha256WrapperMock.Setup(f => f.TransformFinalBlock(It.IsAny<SHA256>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(new byte[0]);
        _sha256WrapperMock.Setup(f => f.GetHash(It.IsAny<SHA256>())).Returns(new byte[3] { 1, 1, 1 });
        _ecdsaWrapperMock.Setup(f => f.VerifyData(It.IsAny<ECDsa>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<HashAlgorithmName>())).Returns(expectedResult);
        bool result = await _target.VerifyFileSignatureAsync(filePath, validSignature);
        Assert.AreEqual(expectedResult, result);
    }
}