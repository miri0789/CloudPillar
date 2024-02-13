using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using System.Text;
using System.Security.Cryptography;
using Shared.Entities.Twin;


namespace CloudPillar.Agent.Tests;

[TestFixture]
public class SignatureHandlerTestFixture
{
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
    private ISignatureHandler _target;
    private Mock<ISHA256Wrapper> _sha256WrapperMock;
    private Mock<IAsymmetricAlgorithmWrapper> _asymmetricAlgorithmWrapperMock;
    private Mock<IServerIdentityHandler> _serverIdentityHandlerMock;
    private DownloadSettings mockDownloadSettingsValue = new DownloadSettings();
    private Mock<IOptions<DownloadSettings>> mockDownloadSettings;
    private const string FILE_EXTENSION = "*.cer";
    private string[] files = new string[] { "pathfile.txt", "pathfile2.txt" };
    private const string SINGATURE_STRING = "AamBQZxGNBGWsm9NkOyWiZRCWGponIRIJo3nnKytRyQlcpJv/iUy5fS1FUodBAX6Sn5kJV9g3DMn2GkJovSWOFXTAdNgY+OJsV42919LetahmaR1M7V8wcHqm+0ddfwF9MzO11fl39PZTT6upInvAVb8KA7Hazjn9enCWCxcise/2RZy";


    [SetUp]
    public void Setup()
    {
        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
        _sha256WrapperMock = new Mock<ISHA256Wrapper>();
        _asymmetricAlgorithmWrapperMock = new Mock<IAsymmetricAlgorithmWrapper>();
        _serverIdentityHandlerMock = new Mock<IServerIdentityHandler>();
        mockDownloadSettingsValue = DownloadSettingsHelper.SetDownloadSettingsValueMock();
        mockDownloadSettings = new Mock<IOptions<DownloadSettings>>();
        mockDownloadSettings.Setup(x => x.Value).Returns(mockDownloadSettingsValue);

        _target = new SignatureHandler(_fileStreamerWrapperMock.Object, _loggerHandlerMock.Object, _d2CMessengerHandlerMock.Object, _sha256WrapperMock.Object, _asymmetricAlgorithmWrapperMock.Object, _serverIdentityHandlerMock.Object, mockDownloadSettings.Object);
        _fileStreamerWrapperMock.Setup(f => f.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(files);
        string publicKeyPem1 = @"-----BEGIN PUBLIC KEY-----
                    MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAESi/IRJco4/1xj3dD+G52BslMo0ZFK
                    4IlL202eiI/cMnUMs4Z7n/icR19JbGZv3URT2cyPjQfRHlSvJ+11XV+lw==
                    -----END PUBLIC KEY-----";
        string publicKeyPem2 = @"-----BEGIN PUBLIC KEY-----
                    MIGbMBAGByqGSM49AgEGBSuBBAAjA4GGAAQBgc4HZz+/fBbC7lmEww0AO3NK9wVZ
                    PDZ0VEnsaUFLEYpTzb90nITtJUcPUbvOsdZIZ1Q8fnbquAYgxXL5UgHMoywAib47
                    6MkyyYgPk0BXZq3mq4zImTRNuaU9slj9TVJ3ScT3L1bXwVuPJDzpr5GOFpaj+WwM
                    Al8G7CqwoJOsW7Kddns=
                    -----END PUBLIC KEY-----";
        _fileStreamerWrapperMock.Setup(f => f.GetFiles("pkipath", FILE_EXTENSION)).Returns(() => new string[] { "pathfile.txt", "pathfile2.txt" });
        _serverIdentityHandlerMock.Setup(s => s.GetPublicKeyFromCertificateFileAsync("pathfile.txt")).ReturnsAsync(() => publicKeyPem1);
        _serverIdentityHandlerMock.Setup(s => s.GetPublicKeyFromCertificateFileAsync("pathfile2.txt")).ReturnsAsync(() => publicKeyPem2);
        _serverIdentityHandlerMock.Setup(s => s.CheckCertificateNotExpired(It.IsAny<string>())).Returns(true);
    }



    [Test]
    public async Task VerifySignature_ValidSignature_ReturnsTrue()
    {
        string message = "value";
        _asymmetricAlgorithmWrapperMock.Setup(f => f.VerifyData(It.IsAny<ECDsa>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<HashAlgorithmName>())).Returns(true);
        bool result = await _target.VerifySignatureAsync(Encoding.UTF8.GetBytes(message), SINGATURE_STRING);
        Assert.IsTrue(result);
    }

    [Test]
    public async Task VerifySignature_ValidSignature_TryAllKeyFiles()
    {
        string message = "value";
        _asymmetricAlgorithmWrapperMock.Setup(f => f.VerifyData(It.IsAny<ECDsa>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<HashAlgorithmName>())).Returns(false);
        bool result = await _target.VerifySignatureAsync(Encoding.UTF8.GetBytes(message), SINGATURE_STRING);
        _asymmetricAlgorithmWrapperMock.Verify(e => e.VerifyData(It.IsAny<ECDsa>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<HashAlgorithmName>()), Times.Exactly(2));
    }

    [Test]
    public async Task VerifySignature_InvalidSignature_ReturnsFalse()
    {
        string message = "Hello, world!";
        string signatureString = "SGVsbG8sIHdvcmxkIQ==";
        bool result = await _target.VerifySignatureAsync(Encoding.UTF8.GetBytes(message), signatureString);
        Assert.IsFalse(result);
    }

    [Test]
    public async Task VerifySignature_CertificateIsExpired_CheckOnlyNonExpiredCertificate()
    {
        _fileStreamerWrapperMock.Setup(x => x.GetFiles(SharedConstants.PKI_FOLDER_PATH, FILE_EXTENSION)).Returns(files);
        _serverIdentityHandlerMock.Setup(x => x.CheckCertificateNotExpired("pathfile.txt")).Returns(true);
        _serverIdentityHandlerMock.Setup(x => x.CheckCertificateNotExpired("pathfile2.txt")).Returns(false);

        _serverIdentityHandlerMock.Setup(x => x.GetPublicKeyFromCertificateFileAsync(It.IsAny<string>())).ReturnsAsync("-----BEGIN RSA PUBLIC KEY-----\r\nSGVsbG8sIFdvcmxkIQ==\r\n-----END RSA PUBLIC KEY-----\r\n");

        _asymmetricAlgorithmWrapperMock.Setup(x => x.GetRSAKey(It.IsAny<byte[]>())).Returns(RSA.Create());
        _asymmetricAlgorithmWrapperMock.Setup(x => x.GetECDsaKey(It.IsAny<byte[]>())).Returns(ECDsa.Create());

        await _target.VerifySignatureAsync(It.IsAny<byte[]>(), SINGATURE_STRING);
        _serverIdentityHandlerMock.Verify(x => x.GetPublicKeyFromCertificateFileAsync(It.IsAny<string>()), Times.Exactly(1));
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
        _asymmetricAlgorithmWrapperMock.Setup(f => f.VerifyData(It.IsAny<ECDsa>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<HashAlgorithmName>())).Returns(expectedResult);
        bool result = await _target.VerifyFileSignatureAsync(filePath, validSignature);
        Assert.AreEqual(expectedResult, result);
    }

    [Test]
    public async Task SendSignTwinKeyEventAsync_ValidData_SendEvent()
    {
        var cts = CancellationToken.None;
        var changeSignKey = "key";

        await _target.SendSignTwinKeyEventAsync(changeSignKey, cts);
        _d2CMessengerHandlerMock.Verify(x => x.SendSignTwinKeyEventAsync(changeSignKey, cts), Times.Once);
    }
}