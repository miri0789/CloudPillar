using Moq;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using System.Text;
using System.Security.Cryptography;
using CloudPillar.Agent.Entities;
using System.Security.Cryptography.X509Certificates;


namespace CloudPillar.Agent.Tests;

[TestFixture]
public class SignatureHandlerTestFixture
{
    private Mock<IFileStreamerWrapper> _fileStreamerWrapperMock;
    private Mock<ILoggerHandler> _loggerHandlerMock;
    private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
    private ISignatureHandler _target;
    private Mock<ISHA256Wrapper> _sha256WrapperMock;
    private Mock<IECDsaWrapper> _ecdsaWrapperMock;
    private DownloadSettings mockDownloadSettingsValue = new DownloadSettings();
    private Mock<IOptions<DownloadSettings>> mockDownloadSettings;
    private Mock<IX509CertificateWrapper> _x509CertificateWrapperMock;
    private Mock<IServerIdentityHandler> _serverIdentityHandlerMock;
    X509Certificate2 x509Certificate1;

    [SetUp]
    public void Setup()
    {
        x509Certificate1 = MockHelper.GenerateCertificate("1", "", 60, "UT_PREFIX");

        _fileStreamerWrapperMock = new Mock<IFileStreamerWrapper>();
        _loggerHandlerMock = new Mock<ILoggerHandler>();
        _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
        _sha256WrapperMock = new Mock<ISHA256Wrapper>();
        _ecdsaWrapperMock = new Mock<IECDsaWrapper>();
        mockDownloadSettingsValue = DownloadSettingsHelper.SetDownloadSettingsValueMock();
        mockDownloadSettings = new Mock<IOptions<DownloadSettings>>();
        mockDownloadSettings.Setup(x => x.Value).Returns(mockDownloadSettingsValue);
        _x509CertificateWrapperMock = new Mock<IX509CertificateWrapper>();
        _serverIdentityHandlerMock = new Mock<IServerIdentityHandler>();
        _target = new SignatureHandler(_fileStreamerWrapperMock.Object, _loggerHandlerMock.Object, _d2CMessengerHandlerMock.Object,
        _sha256WrapperMock.Object, _ecdsaWrapperMock.Object, _serverIdentityHandlerMock.Object, _x509CertificateWrapperMock.Object, mockDownloadSettings.Object);
        _fileStreamerWrapperMock.Setup(f => f.GetFiles(It.IsAny<string>(), It.IsAny<string>())).Returns(new string[] { "pathfile.txt", "pathfile2.txt" });
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

        string ecdsaPublicKeyPem = "-----BEGIN PUBLIC KEY-----\nTUlJQklqQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FROEFNSUlCQ2dLQ0FRRUF2emdhOHgraUl5ckxm\r\nZXJBblB6cEl5dUNPNVBFS1Yzd2dGYWFrOTRrRHNtNlcxcWM3ZHhYNE5yRFpVVDdjTHFDSWl2N3Fh\r\nc3pkK3ZRRHprUUxKcjI0RmQxTkFuT3lsblkxQ0lBTWVTTDdCV09odWJCYVdlTWJWWlQzajFpdkZB\r\nVDI3RGdrVW5SSDg3S0piQi9BVU1SZ3NLYkRzQzZjS1ptb2FPUmZEdjBzbzlOVjdURG5hUmNENkky\r\nUWlWUmxGRzNRTVZGWVoyV3lWQndiYkVsa0FSczBpTHp2NStGVTRWWXc3SHQ0TFB4eFpheG01cjZ4\r\naFBqcjlBUHNGR2FsRW9MTTBFSCtSd3pGcHlMdWFUSTY3SnJOMHBrWDc1MiszYTI3WEh1VE1QRnJW\r\nRnlCTlRzdEZaYUF5VzUzRTBlSGVnTy9vTkxwd3pXRkRseFFXUkU2TDN3TVFJREFRQUI=\n-----END PUBLIC KEY-----\n";
        _fileStreamerWrapperMock.Setup(f => f.ReadAllTextAsync("pathfile.txt")).ReturnsAsync(() => publicKeyPem1);
        _fileStreamerWrapperMock.Setup(f => f.ReadAllTextAsync("pathfile2.txt")).ReturnsAsync(() => publicKeyPem2);
        _x509CertificateWrapperMock.Setup(x => x.CreateFromFile(It.IsAny<string>())).Returns(x509Certificate1);
        _x509CertificateWrapperMock.Setup(x => x.GetECDsaPublicKey(x509Certificate1)).Returns(ECDsa.Create());
        _x509CertificateWrapperMock.Setup(x => x.ExportSubjectPublicKeyInfo(It.IsAny<ECDsa>()))
                    .Returns(Encoding.UTF8.GetBytes("MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAvzga8x+iIyrLferAnPzpIyuCO5PEKV3wgFaak94kDsm6W1qc7dxX4NrDZUT7cLqCIiv7qaszd+vQDzkQLJr24Fd1NAnOylnY1CIAMeSL7BWOhubBaWeMbVZT3j1ivFAT27DgkUnRH87KJbB/AUMRgsKbDsC6cKZmoaORfDv0so9NV7TDnaRcD6I2QiVRlFG3QMVFYZ2WyVBwbbElkARs0iLzv5+FU4VYw7Ht4LPxxZaxm5r6xhPjr9APsFGalEoLM0EH+RwzFpyLuaTI67JrN0pkX752+3a27XHuTMPFrVFyBNTstFZaAyW53E0eHegO/oNLpwzWFDlxQWRE6L3wMQIDAQAB"));
        _serverIdentityHandlerMock.Setup(x => x.GetPublicKeyFromCertificate(It.IsAny<X509Certificate2>()))
                    .ReturnsAsync(ecdsaPublicKeyPem);
        _ecdsaWrapperMock.Setup(x => x.Create()).Returns(ECDsa.Create());
        var publicKeyBytes = Convert.FromBase64String(ecdsaPublicKeyPem);
        var keyReader = new ReadOnlySpan<byte>(publicKeyBytes);
        byte[] byteArray = keyReader.ToArray();
        _ecdsaWrapperMock.Setup(x => x.ImportSubjectPublicKeyInfo(It.IsAny<ECDsa>(), byteArray));
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
    public async Task VerifySignature_ValidSignature_TryAllKeyFiles()
    {
        string message = "value";
        string signatureString = "AamBQZxGNBGWsm9NkOyWiZRCWGponIRIJo3nnKytRyQlcpJv/iUy5fS1FUodBAX6Sn5kJV9g3DMn2GkJovSWOFXTAdNgY+OJsV42919LetahmaR1M7V8wcHqm+0ddfwF9MzO11fl39PZTT6upInvAVb8KA7Hazjn9enCWCxcise/2RZy";
        _ecdsaWrapperMock.Setup(f => f.VerifyData(It.IsAny<ECDsa>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<HashAlgorithmName>())).Returns(false);
        bool result = await _target.VerifySignatureAsync(Encoding.UTF8.GetBytes(message), signatureString);
        _ecdsaWrapperMock.Verify(e => e.VerifyData(It.IsAny<ECDsa>(), It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<HashAlgorithmName>()), Times.Exactly(2));
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