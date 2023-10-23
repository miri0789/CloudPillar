using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Utilities;
using CloudPillar.Agent.Wrappers;
using Moq;
using Shared.Logger;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class X509DPSProvisioningDeviceClientHandlerTestFixture
{
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<IX509CertificateWrapper> _x509CertificateWrapperMock;
    private X509Certificate2 _unitTestCertificate;
    private IDPSProvisioningDeviceClientHandler _target;


    private const string DEVICE_ID = "UnitTest";
    private const string SECRET_KEY = "secert";
    private const string IOT_HUB_HOST_NAME = "IoTHubHostName";


    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILoggerHandler>();
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _x509CertificateWrapperMock = new Mock<IX509CertificateWrapper>();

        _unitTestCertificate = X509Provider.GenerateCertificate(DEVICE_ID, SECRET_KEY, 60);

        _target = new X509DPSProvisioningDeviceClientHandler(_loggerMock.Object, _deviceClientWrapperMock.Object, _x509CertificateWrapperMock.Object);
    }

    [Test]
    public void GetCertificate_ValidCertificateExists_ReturnsCertificate()
    {
        _x509CertificateWrapperMock.Setup(x509 => x509.Certificates).Returns(() =>
        {
            var certificates = new X509Certificate2Collection();
            certificates.Add(_unitTestCertificate);
            return certificates;
        });
        var target = _target.GetCertificate();
        Assert.IsNotNull(target);

    }

    [Test]
    public void GetCertificate_NoValidCertificateExists_ReturnsNull()
    {
        var certificates = new X509Certificate2Collection();
        _x509CertificateWrapperMock.Setup(x509 => x509.Certificates).Returns(certificates);
        var target = _target.GetCertificate();
        Assert.IsNull(target);
    }

    [Test]
    public async Task AuthorizationAsync_ValidCertificateAndParameters_ReturnsTrue()
    {

        _unitTestCertificate.FriendlyName = $"{DEVICE_ID}@{IOT_HUB_HOST_NAME}";
        var XdeviceId = DEVICE_ID;
        var XSecretKey = SECRET_KEY;

        _x509CertificateWrapperMock.Setup(x => x.Certificates)
            .Returns(new X509Certificate2Collection(_unitTestCertificate));

        _deviceClientWrapperMock.Setup(x => x.IsDeviceInitializedAsync(CancellationToken.None)).ReturnsAsync(true);


        var result = await _target.AuthorizationAsync(XdeviceId, XSecretKey, CancellationToken.None);

        Assert.IsTrue(result, "AuthorizationAsync should return true for a valid certificate and parameters.");
    }

    [Test]
    public async Task AuthorizationAsync_InvalidCertificate_ReturnsFalse()
    {
        _x509CertificateWrapperMock.Setup(x => x.Certificates)
            .Returns(new X509Certificate2Collection());

        var result = await _target.AuthorizationAsync(DEVICE_ID, SECRET_KEY, CancellationToken.None);

        // Assert
        Assert.IsFalse(result, "AuthorizationAsync should return false for an invalid certificate.");
    }

    [Test]
    public async Task AuthorizationAsync_InvalidParameters_ReturnsFalse()
    {

        _unitTestCertificate.FriendlyName = "InvalidFriendlyName";
        var XdeviceId = DEVICE_ID;
        var XSecretKey = SECRET_KEY;

        _x509CertificateWrapperMock.Setup(x => x.Certificates)
            .Returns(new X509Certificate2Collection(_unitTestCertificate));

        var result = await _target.AuthorizationAsync(XdeviceId, XSecretKey, CancellationToken.None);

        Assert.IsFalse(result, "AuthorizationAsync should return false for invalid parameters.");
    } 
}