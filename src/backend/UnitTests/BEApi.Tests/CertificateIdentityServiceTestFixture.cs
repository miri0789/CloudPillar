using Backend.BEApi.Services;
using Backend.BEApi.Services.Interfaces;
using Backend.BEApi.Wrappers.Interfaces;
using Moq;
using Shared.Logger;
using Shared.Entities.Twin;
using Backend.Infra.Common.Services.Interfaces;
using Backend.BEApi.Services.interfaces;
using Shared.Entities.Messages;
using System.Security.Cryptography;

namespace Backend.BEApi.Tests;

public class CertificateIdentityServiceTestFixture
{
    private Mock<IEnvironmentsWrapper> _environmentsWrapperMock;
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IHttpRequestorService> _httpRequestorServiceMock;
    private Mock<ITwinDiseredService> _twinDiseredHandlerMock;
    private Mock<ISHA256Wrapper> _sha256WrapperMock;
    private Mock<IChangeSpecService> _changeSpecServiceMock;
    private ICertificateIdentityService _target;
    private const string DEVICE_ID = "deviceId";
    private byte[] publicKey = new byte[] { 1, 2, 3, 4 };
    private byte[] calculateHash = new byte[] { 5, 6, 7, 8 };
    private SHA256 sha526 = SHA256.Create();
    private string cerSign = "cerSign";

    [SetUp]
    public void Setup()
    {
        _environmentsWrapperMock = new Mock<IEnvironmentsWrapper>();
        _loggerMock = new Mock<ILoggerHandler>();
        _httpRequestorServiceMock = new Mock<IHttpRequestorService>();
        _twinDiseredHandlerMock = new Mock<ITwinDiseredService>();
        _sha256WrapperMock = new Mock<ISHA256Wrapper>();
        _changeSpecServiceMock = new Mock<IChangeSpecService>();

        _httpRequestorServiceMock.Setup(x => x.SendRequest<byte[]>(It.Is<string>(u => u.Contains("Signing/GetSigningPublicKeyAsync")), HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>())).ReturnsAsync(publicKey);
        _sha256WrapperMock.Setup(x => x.Create()).Returns(sha526);
        _sha256WrapperMock.Setup(x => x.GetHash(sha526)).Returns(calculateHash);
        _changeSpecServiceMock.Setup(x => x.SendToSignData(It.IsAny<byte[]>(), It.IsAny<string>())).ReturnsAsync(cerSign);

        _target = new CertificateIdentityService(_loggerMock.Object, _environmentsWrapperMock.Object, _httpRequestorServiceMock.Object, _twinDiseredHandlerMock.Object, _sha256WrapperMock.Object, _changeSpecServiceMock.Object);
    }

    [Test]
    public async Task ProcessNewSigningCertificate_ValidProcess_GetSigningPublicKeyAsync()
    {
        await _target.ProcessNewSigningCertificate(DEVICE_ID);
        _httpRequestorServiceMock.Verify(x => x.SendRequest<byte[]>(It.Is<string>(u => u.Contains("Signing/GetSigningPublicKeyAsync")),
         HttpMethod.Get, It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProcessNewSigningCertificate_ValidProcess_UploadCertificateToBlobc()
    {
        await _target.ProcessNewSigningCertificate(DEVICE_ID);
        _httpRequestorServiceMock.Verify(x => x.SendRequest(It.Is<string>(u => u.Contains("blob/UploadStream")), HttpMethod.Post, It.Is<StreamingUploadChunkEvent>(c => c.Data == publicKey), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Test]
    public async Task ProcessNewSigningCertificate_AddRecipeFordownloadCertificate_SignCertificateFile()
    {
        await _target.ProcessNewSigningCertificate(DEVICE_ID);
        _changeSpecServiceMock.Verify(x => x.SendToSignData(It.IsAny<byte[]>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ProcessNewSigningCertificate__ValidProcess_AddRecipeFordownloadCertificate()
    {
        await _target.ProcessNewSigningCertificate(DEVICE_ID);

        _twinDiseredHandlerMock.Verify(x => x.AddDesiredRecipeAsync(It.IsAny<string>(), SharedConstants.CHANGE_SPEC_SERVER_IDENTITY_NAME, It.Is<DownloadAction>(c => c.Sign == cerSign),
            It.IsAny<int>(), It.IsAny<string>()), Times.Once);
    }
    [Test]
    public async Task ProcessNewSigningCertificate_ValidProcess_UpdateChangeSpecSign()
    {
        await _target.ProcessNewSigningCertificate(DEVICE_ID);

        _changeSpecServiceMock.Verify(x => x.CreateChangeSpecKeySignatureAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TwinDesired>()), Times.Once);
    }
}