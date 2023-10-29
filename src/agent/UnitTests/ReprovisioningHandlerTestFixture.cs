using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Utilities;
using CloudPillar.Agent.Wrappers;
using Microsoft.Azure.Devices.Provisioning.Service;
using Moq;
using Newtonsoft.Json;
using Shared.Entities.Authentication;
using Shared.Entities.Messages;
using Shared.Logger;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class ReprovisioningHandlerTestFixture
{
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<IX509CertificateWrapper> _x509CertificateWrapperMock;
    private Mock<IDPSProvisioningDeviceClientHandler> _dPSProvisioningDeviceClientHandlerMock;
    private Mock<IEnvironmentsWrapper> _environmentsWrapperMock;
    private Mock<ID2CMessengerHandler> _d2CMessengerHandlerMock;
    private Mock<ISHA256Wrapper> _sHA256WrapperMock;
    private Mock<IProvisioningServiceClientWrapper> _provisioningServiceClientWrapperMock;
    private IReprovisioningHandler _target;
    private const string DEVICE_ID = "UnitTest";
    private const string SECRET_KEY = "secert";
    private const string IOTHUB_HOST_NAME = "IotHubHostName";
    private ReprovisioningMessage _validReprovisioningMessage;

    private X509Certificate2 _certificate;


    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILoggerHandler>();
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _x509CertificateWrapperMock = new Mock<IX509CertificateWrapper>();
        _dPSProvisioningDeviceClientHandlerMock = new Mock<IDPSProvisioningDeviceClientHandler>();
        _environmentsWrapperMock = new Mock<IEnvironmentsWrapper>();
        _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();
        _sHA256WrapperMock = new Mock<ISHA256Wrapper>();
        _provisioningServiceClientWrapperMock = new Mock<IProvisioningServiceClientWrapper>();

        _provisioningServiceClientWrapperMock.Setup(p => p.GetIndividualEnrollmentAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None))
        .ReturnsAsync(() =>
        {
            return new IndividualEnrollment("", new SymmetricKeyAttestation("", "")) { IotHubHostName = IOTHUB_HOST_NAME };
        });

        _validReprovisioningMessage = new ReprovisioningMessage()
        {
            Data = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(new AuthonticationKeys()
            {
                DeviceId = DEVICE_ID,
                SecretKey = SECRET_KEY
            })),
            DPSConnectionString = "dpsConnectionString"
        };

        _certificate = X509Provider.GenerateCertificate(DEVICE_ID, SECRET_KEY, 60);
        _x509CertificateWrapperMock.Setup(x => x.CreateFromBytes(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<X509KeyStorageFlags>())).Returns(_certificate);
        _sHA256WrapperMock.Setup(x => x.ComputeHash(It.IsAny<byte[]>())).Returns(Encoding.UTF8.GetBytes("hash"));
        _d2CMessengerHandlerMock.Setup(x => x.ProvisionDeviceCertificateEventAsync(_certificate)).Returns(Task.CompletedTask);




        _target = new ReprovisioningHandler(_deviceClientWrapperMock.Object,
        _x509CertificateWrapperMock.Object,
        _dPSProvisioningDeviceClientHandlerMock.Object,
        _environmentsWrapperMock.Object,
        _d2CMessengerHandlerMock.Object,
        _sHA256WrapperMock.Object,
        _provisioningServiceClientWrapperMock.Object,
        _loggerMock.Object);
    }

    [Test]
    public async Task HandleReprovisioningMessageAsync_ValidMessage_CallsProvisioningMethod()
    {
        _certificate.FriendlyName = CertificateConstants.TEMPORARY_CERTIFICATE_NAME;
        SetupX509CertificateWrapperMock();
        _dPSProvisioningDeviceClientHandlerMock.Setup(x => x.ProvisioningAsync(It.IsAny<string>(), _certificate, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _target.HandleReprovisioningMessageAsync(_validReprovisioningMessage, CancellationToken.None);

        _dPSProvisioningDeviceClientHandlerMock.Verify(x => x.ProvisioningAsync(It.IsAny<string>(), _certificate, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task HandleReprovisioningMessageAsync_InvalidMessage_ThrowException()
    {
        var message = new ReprovisioningMessage();

        _certificate.FriendlyName = CertificateConstants.TEMPORARY_CERTIFICATE_NAME;



        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.HandleReprovisioningMessageAsync(message, CancellationToken.None));

    }
    [Test]
    public async Task HandleReprovisioningMessageAsync_InvalidCertificate_ThrowException()
    {

        _certificate.FriendlyName = "invalidCertificate";
        SetupX509CertificateWrapperMock();

        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.HandleReprovisioningMessageAsync(_validReprovisioningMessage, CancellationToken.None));

    }

    [Test]
    public async Task HandleReprovisioningMessageAsync_Valid_CertificateFriendlyNameChanged()
    {
        _certificate.FriendlyName = CertificateConstants.TEMPORARY_CERTIFICATE_NAME;
        SetupX509CertificateWrapperMock();
        await _target.HandleReprovisioningMessageAsync(_validReprovisioningMessage, CancellationToken.None);

        Assert.AreEqual(_certificate.FriendlyName, $"{DEVICE_ID}@{IOTHUB_HOST_NAME}");

    }

    [Test]
    public async Task HandleRequestDeviceCertificateAsync_ValidMessage_SendsMessage()
    {
        var message = new RequestDeviceCertificateMessage
        {
            Data = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(new AuthonticationKeys { DeviceId = DEVICE_ID, SecretKey = SECRET_KEY })),
        };

        await _target.HandleRequestDeviceCertificateAsync(message, CancellationToken.None);

        _d2CMessengerHandlerMock.Verify(d => d.ProvisionDeviceCertificateEventAsync(It.IsAny<X509Certificate2>()), Times.Once);
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


    private void SetupX509CertificateWrapperMock()
    {
        _x509CertificateWrapperMock.Setup(x => x.Open(OpenFlags.ReadWrite));
        _x509CertificateWrapperMock.Setup(x => x.GetCertificates(It.IsAny<X509Store>())).Returns(new X509Certificate2Collection() { _certificate });
        _x509CertificateWrapperMock.Setup(x => x.Find(It.IsAny<X509Store>(), It.IsAny<X509FindType>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(new X509Certificate2Collection(_certificate));
    }


}