using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Moq;
using CloudPillar.Agent.Handlers.Logger;
using Microsoft.Extensions.Options;
using CloudPillar.Agent.Enums;

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
    private Mock<IOptions<AuthenticationSettings>> _authenticationSettingsMock;
    private Mock<ITwinReportHandler> _twinReportHandlerMock;

    private const string DEVICE_ID = "UnitTest";
    private const string SECRET_KEY = "secert";
    private const string IOT_HUB_HOST_NAME = "IoTHubHostName";
    const string DPS_SCOPE_ID = "dpsScopeId";
    const string GLOBAL_DEVICE_ENDPOINT = "globalDeviceEndpoint";
    private const string CERTIFICATE_PREFIX = "CP";
    private const string ENVITOMENT = "UnitTest";

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILoggerHandler>();
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _x509CertificateWrapperMock = new Mock<IX509CertificateWrapper>();
        _provisioningDeviceClientWrapperMock = new Mock<IProvisioningDeviceClientWrapper>();
        _twinReportHandlerMock = new Mock<ITwinReportHandler>();

        _unitTestCertificate = MockHelper.GenerateCertificate(DEVICE_ID, SECRET_KEY, GetCertificatePrefix(), 60);
        _unitTestCertificate.FriendlyName = $"{DEVICE_ID}@{IOT_HUB_HOST_NAME}";
        _x509CertificateWrapperMock.Setup(x => x.GetSecurityProvider(_unitTestCertificate)).Returns(new SecurityProviderX509Certificate(_unitTestCertificate));
        _deviceClientWrapperMock.Setup(x => x.GetProvisioningTransportHandler()).Returns(Mock.Of<ProvisioningTransportHandler>());

        _authenticationSettingsMock = new Mock<IOptions<AuthenticationSettings>>();
        _authenticationSettingsMock.Setup(x => x.Value).Returns(new AuthenticationSettings() { CertificatePrefix = CERTIFICATE_PREFIX, Environment = ENVITOMENT, StoreLocation = StoreLocation.LocalMachine});

        _x509CertificateWrapperMock.Setup(x => x.Open(OpenFlags.ReadOnly, StoreLocation.LocalMachine, StoreName.My));
        _x509CertificateWrapperMock.Setup(x => x.GetCertificates(It.IsAny<X509Store>())).Returns(Mock.Of<X509Certificate2Collection>());
        _deviceClientWrapperMock.Setup(x => x.DeviceInitializationAsync(It.IsAny<string>(), It.IsAny<DeviceAuthenticationWithX509Certificate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _deviceClientWrapperMock.Setup(x => x.IsDeviceInitializedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(DeviceConnectionResult.Valid);

        CreateTarget();
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
    public void GetCertificate_NoMatchCertificatePrefix_ReturnsNull()
    {
        _authenticationSettingsMock.Setup(x => x.Value).Returns(new AuthenticationSettings() { CertificatePrefix = "NoValidUnitTest-CP-" });
        CreateTarget();

        var target = _target.GetCertificate();
        Assert.IsNull(target);

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

        _deviceClientWrapperMock.Setup(x => x.IsDeviceInitializedAsync(CancellationToken.None)).ReturnsAsync(DeviceConnectionResult.Valid);


        var result = await _target.AuthorizationDeviceAsync(XdeviceId, XSecretKey, CancellationToken.None);

        Assert.AreEqual(result, DeviceConnectionResult.Valid, "AuthorizationDeviceAsync should return true for a valid certificate and parameters");
    }

    [Test]
    public async Task AuthorizationDeviceAsync_InvalidCertificate_ReturnsFalse()
    {
        _x509CertificateWrapperMock.Setup(x => x.GetCertificates(It.IsAny<X509Store>()))
            .Returns(new X509Certificate2Collection());

        var result = await _target.AuthorizationDeviceAsync(DEVICE_ID, SECRET_KEY, CancellationToken.None);

        // Assert
        Assert.AreNotEqual(result, DeviceConnectionResult.Valid, "AuthorizationDeviceAsync should return false for an invalid certificate.");
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

        Assert.AreNotEqual(result, DeviceConnectionResult.Valid, "AuthorizationDeviceAsync should return false for invalid parameters.");
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
    private string GetCertificatePrefix()
    {
        return !string.IsNullOrEmpty(ENVITOMENT) ? $"{CERTIFICATE_PREFIX}-{ENVITOMENT}-" : $"{CERTIFICATE_PREFIX}-";
    }

    private void CreateTarget()
    {
        _target = new X509DPSProvisioningDeviceClientHandler(_loggerMock.Object, _deviceClientWrapperMock.Object, _x509CertificateWrapperMock.Object, _provisioningDeviceClientWrapperMock.Object, _authenticationSettingsMock.Object, _twinReportHandlerMock.Object);
    }
}