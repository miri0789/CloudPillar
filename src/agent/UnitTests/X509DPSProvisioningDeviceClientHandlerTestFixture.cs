using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Utilities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Moq;
using Shared.Logger;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class X509DPSProvisioningDeviceClientHandlerTestFixture
{
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<IX509CertificateWrapper> _x509CertificateWrapperMock;
    private Mock<IProvisioningDeviceClientWrapper> _provisioningDeviceClientWrapperMock;
    private X509Certificate2 _unitTestCertificate;
    private IDPSProvisioningDeviceClientHandler _target;


    private const string DEVICE_ID = "UnitTest";
    private const string SECRET_KEY = "secert";
    private const string IOT_HUB_HOST_NAME = "IoTHubHostName";
    const string DPS_SCOPE_ID = "dpsScopeId";
    const string GLOBAL_DEVICE_ENDPOINT = "globalDeviceEndpoint";


    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILoggerHandler>();
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _x509CertificateWrapperMock = new Mock<IX509CertificateWrapper>();
        _provisioningDeviceClientWrapperMock = new Mock<IProvisioningDeviceClientWrapper>();

        _unitTestCertificate = X509Provider.GenerateCertificate(DEVICE_ID, SECRET_KEY, 60);
        _unitTestCertificate.FriendlyName = $"{DEVICE_ID}@{IOT_HUB_HOST_NAME}";
        _x509CertificateWrapperMock.Setup(x => x.GetSecurityProvider(_unitTestCertificate)).Returns(new SecurityProviderX509Certificate(_unitTestCertificate));
        _deviceClientWrapperMock.Setup(x => x.GetProvisioningTransportHandler()).Returns(Mock.Of<ProvisioningTransportHandler>());

        _x509CertificateWrapperMock.Setup(x => x.Open(OpenFlags.ReadOnly, StoreName.My));
        _x509CertificateWrapperMock.Setup(x => x.GetCertificates(It.IsAny<X509Store>())).Returns(Mock.Of<X509Certificate2Collection>());
        _deviceClientWrapperMock.Setup(x => x.DeviceInitializationAsync(It.IsAny<string>(), It.IsAny<DeviceAuthenticationWithX509Certificate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _deviceClientWrapperMock.Setup(x => x.IsDeviceInitializedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _target = new X509DPSProvisioningDeviceClientHandler(_loggerMock.Object, _deviceClientWrapperMock.Object, _x509CertificateWrapperMock.Object, _provisioningDeviceClientWrapperMock.Object);
    }

    [Test]
    public void GetCertificate_ValidCertificateExists_ReturnsCertificate()
    {
        _x509CertificateWrapperMock.Setup(x509 => x509.GetCertificates(It.IsAny<X509Store>())).Returns(() =>
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
        _x509CertificateWrapperMock.Setup(x509 => x509.GetCertificates(It.IsAny<X509Store>())).Returns(certificates);
        var target = _target.GetCertificate();
        Assert.IsNull(target);
    }

    [Test]
    public async Task AuthorizationDeviceAsync_ValidCertificateAndParameters_ReturnsTrue()
    {

        _unitTestCertificate.FriendlyName = $"{DEVICE_ID}@{IOT_HUB_HOST_NAME}";
        var XdeviceId = DEVICE_ID;
        var XSecretKey = SECRET_KEY;

        _x509CertificateWrapperMock.Setup(x => x.GetCertificates(It.IsAny<X509Store>()))
            .Returns(new X509Certificate2Collection(_unitTestCertificate));

        _deviceClientWrapperMock.Setup(x => x.IsDeviceInitializedAsync(CancellationToken.None)).ReturnsAsync(true);


        var result = await _target.AuthorizationDeviceAsync(XdeviceId, XSecretKey, CancellationToken.None);

        Assert.IsTrue(result, "AuthorizationDeviceAsync should return true for a valid certificate and parameters.");
    }

    [Test]
    public async Task AuthorizationDeviceAsync_InvalidCertificate_ReturnsFalse()
    {
        _x509CertificateWrapperMock.Setup(x => x.GetCertificates(It.IsAny<X509Store>()))
            .Returns(new X509Certificate2Collection());

        var result = await _target.AuthorizationDeviceAsync(DEVICE_ID, SECRET_KEY, CancellationToken.None);

        // Assert
        Assert.IsFalse(result, "AuthorizationDeviceAsync should return false for an invalid certificate.");
    }

    [Test]
    public async Task AuthorizationDeviceAsync_InvalidParameters_ReturnsFalse()
    {

        _unitTestCertificate.FriendlyName = "InvalidFriendlyName";
        var XdeviceId = DEVICE_ID;
        var XSecretKey = SECRET_KEY;

        _x509CertificateWrapperMock.Setup(x => x.GetCertificates(It.IsAny<X509Store>()))
            .Returns(new X509Certificate2Collection(_unitTestCertificate));

        var result = await _target.AuthorizationDeviceAsync(XdeviceId, XSecretKey, CancellationToken.None);

        Assert.IsFalse(result, "AuthorizationDeviceAsync should return false for invalid parameters.");
    }

    [Test]
    public async Task ProvisioningAsync_ValidParameters_RegistersDevice()
    {

        _provisioningDeviceClientWrapperMock.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecurityProvider>(), It.IsAny<ProvisioningTransportHandler>())).ReturnsAsync(() =>
    {
        return new DeviceRegistrationResult(DEVICE_ID, null, IOT_HUB_HOST_NAME, DEVICE_ID, ProvisioningRegistrationStatusType.Assigned, "generationId", null, 0, string.Empty, string.Empty);
    });

        await _target.ProvisioningAsync(DPS_SCOPE_ID, _unitTestCertificate, GLOBAL_DEVICE_ENDPOINT, It.IsAny<Message>(), CancellationToken.None);

        _deviceClientWrapperMock.Verify(x => x.DeviceInitializationAsync(It.IsAny<string>(), It.IsAny<DeviceAuthenticationWithX509Certificate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ProvisioningAsync_RegisterFaild_RegistersDeviceNotCalled()
    {
        _provisioningDeviceClientWrapperMock.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecurityProvider>(), It.IsAny<ProvisioningTransportHandler>())).ReturnsAsync(() =>
    {
        return new DeviceRegistrationResult(DEVICE_ID, null, IOT_HUB_HOST_NAME, DEVICE_ID, ProvisioningRegistrationStatusType.Failed, "generationId", null, 0, string.Empty, string.Empty);
    });

        await _target.ProvisioningAsync(DPS_SCOPE_ID, _unitTestCertificate, GLOBAL_DEVICE_ENDPOINT, It.IsAny<Message>(), CancellationToken.None);

        _deviceClientWrapperMock.Verify(x => x.DeviceInitializationAsync(It.IsAny<string>(), It.IsAny<DeviceAuthenticationWithX509Certificate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task ProvisioningAsync_InvalidParameters_ThrowException()
    {
        _provisioningDeviceClientWrapperMock.Setup(x => x.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<SecurityProvider>(), It.IsAny<ProvisioningTransportHandler>())).ReturnsAsync(() =>
        {
            return new DeviceRegistrationResult(DEVICE_ID, null, IOT_HUB_HOST_NAME, DEVICE_ID, ProvisioningRegistrationStatusType.Failed, "generationId", null, 0, string.Empty, string.Empty);
        });
        Assert.ThrowsAsync<ArgumentException>(async () => await _target.ProvisioningAsync(string.Empty, _unitTestCertificate, string.Empty, It.IsAny<Message>(), CancellationToken.None));

    }
}