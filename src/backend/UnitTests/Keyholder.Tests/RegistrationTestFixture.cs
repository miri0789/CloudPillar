using System.Security.Cryptography;
using Microsoft.Azure.Devices;
using Moq;
using Shared.Logger;
using Backend.Keyholder.Wrappers.Interfaces;
using Shared.Entities.Factories;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Provisioning.Service;
using Backend.Infra.Common.Services.Interfaces;


namespace Backend.Keyholder.Tests;


public class RegistrationTestFixture
{
    private Mock<IMessageFactory> _messageFactoryMock;
    private Mock<IDeviceConnectService> _deviceConnectServiceMock;
    private Mock<IIndividualEnrollmentWrapper> _individualEnrollmentWrapperMock;
    private Mock<IX509CertificateWrapper> _x509CertificateWrapperMock;
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IEnvironmentsWrapper> _environmentsWrapperMock;
    private Mock<IProvisioningServiceClientWrapper> _provisioningServiceClientWrapperMock;
    private IRegistrationService _target;

    private const string DEVICE_ID = "deviceId";
    private const string SECRET_KEY = "secretKey";
    private const string CERTIFICATE_PREFIX = "UnitTest-CP-";


    [SetUp]
    public void Setup()
    {
        _messageFactoryMock = new Mock<IMessageFactory>();
        _deviceConnectServiceMock = new Mock<IDeviceConnectService>();
        _individualEnrollmentWrapperMock = new Mock<IIndividualEnrollmentWrapper>();
        _x509CertificateWrapperMock = new Mock<IX509CertificateWrapper>();
        _loggerMock = new Mock<ILoggerHandler>();
        _environmentsWrapperMock = new Mock<IEnvironmentsWrapper>();
        _provisioningServiceClientWrapperMock = new Mock<IProvisioningServiceClientWrapper>();
        _environmentsWrapperMock.Setup(c => c.dpsConnectionString).Returns("dpsConnectionString");
        _environmentsWrapperMock.Setup(c => c.iothubConnectionString).Returns("HostName=unitTest;SharedAccessKeyName=iothubowner;");

        var enrollment = new IndividualEnrollment(CERTIFICATE_PREFIX, new SymmetricKeyAttestation("", ""));
        _x509CertificateWrapperMock.Setup(x => x.CreateCertificate(It.IsAny<byte[]>())).Returns(new X509Certificate2(GenerateCertificate().Export(X509ContentType.Cert)));
        _individualEnrollmentWrapperMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<Attestation>())).Returns(enrollment);
        _provisioningServiceClientWrapperMock.Setup(x => x.CreateOrUpdateIndividualEnrollmentAsync(It.IsAny<ProvisioningServiceClient>(), It.IsAny<IndividualEnrollment>())).ReturnsAsync(enrollment);


        _target = new RegistrationService(_messageFactoryMock.Object,
         _deviceConnectServiceMock.Object,
          _environmentsWrapperMock.Object,
           _individualEnrollmentWrapperMock.Object,
            _x509CertificateWrapperMock.Object,
            _provisioningServiceClientWrapperMock.Object,
             _loggerMock.Object);
    }

    [Test]
    public async Task RegisterAsync_ValidParameters_MessageSendToAgent()
    {
        await _target.RegisterAsync(DEVICE_ID, SECRET_KEY);

        _deviceConnectServiceMock.Verify(x => x.SendDeviceMessageAsync(It.IsAny<Message>(), It.IsAny<string>()), Times.Once);

    }

    [Test]
    public async Task RegisterAsync_InvalidDeviceIdParameter_ThrowException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await _target.RegisterAsync(string.Empty, SECRET_KEY));

    }


    [Test]
    public async Task RegisterAsync_InvalidSecretKeyParameter_ThrowException()
    {
        Assert.ThrowsAsync<ArgumentException>(async () => await _target.RegisterAsync(DEVICE_ID, string.Empty));

    }


    [Test]
    public async Task ProvisionDeviceCertificateAsync_ValidParameter_MessageSendToAgent()
    {
        await _target.ProvisionDeviceCertificateAsync(DEVICE_ID, CERTIFICATE_PREFIX, new byte[100]);

        _deviceConnectServiceMock.Verify(x => x.SendDeviceMessageAsync(It.IsAny<Message>(), It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task ProvisionDeviceCertificateAsync_CertificatePrefix_CreateEntollmentWithPrefix()
    {
        await _target.ProvisionDeviceCertificateAsync(DEVICE_ID, CERTIFICATE_PREFIX, new byte[100]);

        _provisioningServiceClientWrapperMock.Verify(x => x.CreateOrUpdateIndividualEnrollmentAsync(It.IsAny<ProvisioningServiceClient>(), It.Is<IndividualEnrollment>(y => y.RegistrationId.StartsWith(CERTIFICATE_PREFIX))), Times.Once);
    }

    [Test]
    public async Task ProvisionDeviceCertificateAsync_InvalidCertificateParameter_ThrowException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.ProvisionDeviceCertificateAsync(DEVICE_ID, CERTIFICATE_PREFIX, null));

    }
    [Test]
    public async Task ProvisionDeviceCertificateAsync_InvalidDeviceIdParameter_ThrowException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.ProvisionDeviceCertificateAsync(null, CERTIFICATE_PREFIX, new byte[100]));
    }

    private X509Certificate2 GenerateCertificate()
    {
        using (RSA rsa = RSA.Create(4096))
        {
            var request = new CertificateRequest(
                $"CN={CERTIFICATE_PREFIX}{DEVICE_ID}", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddDays(1));
            return certificate;

        }
    }
}
