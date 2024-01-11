using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Shared.Entities.Authentication;
using Shared.Entities.Messages;
using CloudPillar.Agent.Handlers.Logger;
using CloudPillar.Agent.Sevices;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class ReprovisioningHandlerTestFixture
{
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IX509CertificateWrapper> _x509CertificateWrapperMock;
    private Mock<IDPSProvisioningDeviceClientHandler> _dPSProvisioningDeviceClientHandlerMock;
    private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
    private Mock<ISHA256Wrapper> _sHA256WrapperMock;
    private Mock<IProvisioningServiceClientWrapper> _provisioningServiceClientWrapperMock;
    private Mock<IOptions<AuthenticationSettings>> _authenticationSettingsMock;
    private Mock<RemoveX509Certificates> _removeX509CertificatesMock;
    private Mock<IX509Provider> _x509ProviderMock;
    private IReprovisioningHandler _target;
    private const string DEVICE_ID = "UnitTest";
    private const string SECRET_KEY = "secert";
    private const string IOTHUB_HOST_NAME = "IotHubHostName";
    private const string CERTIFICATE_PREFIX = "CP";
    private const string ENVITOMENT = "UnitTest";
    private ReprovisioningMessage _validReprovisioningMessage;

    private X509Certificate2 _certificate;
    private string temporaryCertificateName;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILoggerHandler>();
        _x509CertificateWrapperMock = new Mock<IX509CertificateWrapper>();
        _dPSProvisioningDeviceClientHandlerMock = new Mock<IDPSProvisioningDeviceClientHandler>();
        _authenticationSettingsMock = new Mock<IOptions<AuthenticationSettings>>();
        _authenticationSettingsMock.Setup(x => x.Value).Returns(new AuthenticationSettings() { CertificatePrefix = CERTIFICATE_PREFIX, Environment = ENVITOMENT });
        _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
        _sHA256WrapperMock = new Mock<ISHA256Wrapper>();
        _provisioningServiceClientWrapperMock = new Mock<IProvisioningServiceClientWrapper>();
        _x509ProviderMock = new Mock<IX509Provider>();
        _removeX509CertificatesMock = new Mock<RemoveX509Certificates>();

        _provisioningServiceClientWrapperMock.Setup(p => p.GetIndividualEnrollmentAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
        .ReturnsAsync(() =>
        {
            return new IndividualEnrollment("", new SymmetricKeyAttestation("", "")) { IotHubHostName = IOTHUB_HOST_NAME };
        });

        _validReprovisioningMessage = new ReprovisioningMessage()
        {
            Data = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(new AuthenticationKeys()
            {
                DeviceId = DEVICE_ID,
                SecretKey = SECRET_KEY
            })),
            DPSConnectionString = "dpsConnectionString"
        };

        _certificate = MockHelper.GenerateCertificate(DEVICE_ID, SECRET_KEY, 60, GetCertificatePrefix());
        _x509CertificateWrapperMock.Setup(x => x.CreateFromBytes(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<X509KeyStorageFlags>())).Returns(_certificate);
        _sHA256WrapperMock.Setup(x => x.ComputeHash(It.IsAny<byte[]>())).Returns(Encoding.UTF8.GetBytes("hash"));
        _d2CMessengerHandlerMock.Setup(x => x.ProvisionDeviceCertificateEventAsync(GetCertificatePrefix(), _certificate, CancellationToken.None)).Returns(Task.CompletedTask);

        _target = new ReprovisioningHandler(
        _x509CertificateWrapperMock.Object,
        _dPSProvisioningDeviceClientHandlerMock.Object,
        _d2CMessengerHandlerMock.Object,
        _sHA256WrapperMock.Object,
        _provisioningServiceClientWrapperMock.Object,
        _authenticationSettingsMock.Object,
        _x509ProviderMock.Object,
        _removeX509CertificatesMock.Object,
        _loggerMock.Object);

        temporaryCertificateName = $"{GetCertificatePrefix()}{ProvisioningConstants.TEMPORARY_CERTIFICATE_NAME}";

    }

    [Test]
    public async Task HandleReprovisioningMessageAsync_ValidMessage_CallsProvisioningMethod()
    {
        _certificate.FriendlyName = temporaryCertificateName;
        SetupX509CertificateWrapperMock();
        _dPSProvisioningDeviceClientHandlerMock.Setup(x => x.ProvisioningAsync(It.IsAny<string>(), _certificate, It.IsAny<string>(), It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _target.HandleReprovisioningMessageAsync(It.IsAny<Message>(), _validReprovisioningMessage, CancellationToken.None);

        _dPSProvisioningDeviceClientHandlerMock.Verify(x => x.ProvisioningAsync(It.IsAny<string>(), _certificate, It.IsAny<string>(), It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleReprovisioningMessageAsync_InvalidMessage_ThrowException()
    {
        var message = new ReprovisioningMessage();

        _certificate.FriendlyName = temporaryCertificateName;

        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.HandleReprovisioningMessageAsync(It.IsAny<Message>(), message, CancellationToken.None));
    }

    [Test]
    public async Task HandleReprovisioningMessageAsync_InvalidCertificatePrefix_ThrowException()
    {

        _certificate.FriendlyName = ProvisioningConstants.TEMPORARY_CERTIFICATE_NAME;
        SetupX509CertificateWrapperMock();

        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.HandleReprovisioningMessageAsync(It.IsAny<Message>(), It.IsAny<ReprovisioningMessage>(), CancellationToken.None));
    }

    [Test]
    public async Task HandleReprovisioningMessageAsync_InvalidCertificate_ThrowException()
    {

        _certificate.FriendlyName = "invalidCertificate";
        SetupX509CertificateWrapperMock();

        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.HandleReprovisioningMessageAsync(It.IsAny<Message>(), _validReprovisioningMessage, CancellationToken.None));

    }

    [Test]
    public async Task HandleReprovisioningMessageAsync_Valid_CertificateFriendlyNameChanged()
    {
        _certificate.FriendlyName = temporaryCertificateName;
        SetupX509CertificateWrapperMock();
        await _target.HandleReprovisioningMessageAsync(It.IsAny<Message>(), _validReprovisioningMessage, CancellationToken.None);

        Assert.AreEqual(_certificate.FriendlyName, $"{DEVICE_ID}@{IOTHUB_HOST_NAME}");

    }

    [Test]
    public async Task HandleRequestDeviceCertificateAsync_ValidMessage_SendsMessage()
    {
        _x509ProviderMock.Setup(x => x.GenerateCertificate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(_certificate);
        var message = new RequestDeviceCertificateMessage
        {
            Data = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(new AuthenticationKeys { DeviceId = DEVICE_ID, SecretKey = SECRET_KEY })),
        };

        await _target.HandleRequestDeviceCertificateAsync(message, CancellationToken.None);

        _d2CMessengerHandlerMock.Verify(d => d.ProvisionDeviceCertificateEventAsync(It.IsAny<string>(), It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleRequestDeviceCertificateAsync_InvalidMessage_ThrowException()
    {
        var message = new RequestDeviceCertificateMessage();

        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.HandleRequestDeviceCertificateAsync(message, CancellationToken.None));
    }

    [Test]
    public async Task HandleRequestDeviceCertificateAsync_InvalidDataInMessage_ThrowException()
    {
        var message = new RequestDeviceCertificateMessage()
        {
            Data = null
        };

        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.HandleRequestDeviceCertificateAsync(message, CancellationToken.None));
    }

    [Test]
    public async Task HandleRequestDeviceCertificateAsync_CertificateIsNull_ThrowException()
    {
        _x509ProviderMock.Setup(x => x.GenerateCertificate(GetCertificatePrefix(), It.IsAny<string>(), It.IsAny<int>())).Returns(null as X509Certificate2);
        var message = new RequestDeviceCertificateMessage
        {
            Data = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(new AuthenticationKeys { DeviceId = DEVICE_ID, SecretKey = SECRET_KEY })),
        };

        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.HandleRequestDeviceCertificateAsync(message, CancellationToken.None));

    }

    [Test]
    public async Task HandleRequestDeviceCertificateAsync_CertificatePrefix_ValidPrefix()
    {
        _x509ProviderMock.Setup(x => x.GenerateCertificate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).Returns(_certificate);
        var message = new RequestDeviceCertificateMessage
        {
            Data = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(new AuthenticationKeys { DeviceId = DEVICE_ID, SecretKey = SECRET_KEY })),
        };

        await _target.HandleRequestDeviceCertificateAsync(message, CancellationToken.None);

        _d2CMessengerHandlerMock.Verify(d => d.ProvisionDeviceCertificateEventAsync(It.Is<string>(x => x == GetCertificatePrefix()), It.IsAny<X509Certificate2>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private string GetCertificatePrefix()
    {
        return !string.IsNullOrEmpty(ENVITOMENT) ? $"{CERTIFICATE_PREFIX}-{ENVITOMENT}-" : $"{CERTIFICATE_PREFIX}-";
    }

    private void SetupX509CertificateWrapperMock()
    {
        _x509CertificateWrapperMock.Setup(x => x.Open(OpenFlags.ReadWrite, StoreLocation.LocalMachine, StoreName.My));
        _x509CertificateWrapperMock.Setup(x => x.GetCertificates(It.IsAny<X509Store>())).Returns(new X509Certificate2Collection() { _certificate });
        _x509CertificateWrapperMock.Setup(x => x.Find(It.IsAny<X509Store>(), It.IsAny<X509FindType>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(new X509Certificate2Collection(_certificate));
    }


}