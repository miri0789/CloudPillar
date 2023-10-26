using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Devices;
using Moq;
using k8s;
using k8s.Models;
using Microsoft.Azure.Devices.Shared;
using Backend.Keyholder.Interfaces;
using Shared.Logger;
using Backend.Keyholder.Services;
using Backend.Keyholder.Wrappers.Interfaces;
using Shared.Entities.Factories;
using Shared.Entities.Messages;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Devices.Provisioning.Service;
using common;

namespace Backend.Keyholder.Tests;


public class RegistrationTestFixture
{
    private Mock<IMessageFactory> _messageFactoryMock;
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<IIndividualEnrollmentWrapper> _individualEnrollmentWrapperMock;
    private Mock<IX509CertificateWrapper> _x509CertificateWrapperMock;
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IEnvironmentsWrapper> _environmentsWrapperMock;
    private Mock<IProvisioningServiceClientWrapper> _provisioningServiceClientWrapperMock;
    private IRegistrationService _target;

    private const string DEVICE_ID = "deviceId";
    private const string SECRET_KEY = "secretKey";


    [SetUp]
    public void Setup()
    {
        _messageFactoryMock = new Mock<IMessageFactory>();
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _individualEnrollmentWrapperMock = new Mock<IIndividualEnrollmentWrapper>();
        _x509CertificateWrapperMock = new Mock<IX509CertificateWrapper>();
        _loggerMock = new Mock<ILoggerHandler>();
        _environmentsWrapperMock = new Mock<IEnvironmentsWrapper>();
        _provisioningServiceClientWrapperMock = new Mock<IProvisioningServiceClientWrapper>();
        _environmentsWrapperMock.Setup(c => c.dpsConnectionString).Returns("dpsConnectionString");
        _environmentsWrapperMock.Setup(c => c.iothubConnectionString).Returns("HostName=unitTest;SharedAccessKeyName=iothubowner;");


        _target = new RegistrationService(_messageFactoryMock.Object,
         _deviceClientWrapperMock.Object,
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

        _deviceClientWrapperMock.Verify(x => x.SendAsync(It.IsAny<ServiceClient>(), It.IsAny<string>(), It.IsAny<Message>()), Times.Once);

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
        var enrollment = new IndividualEnrollment("", new SymmetricKeyAttestation("", ""));
        _x509CertificateWrapperMock.Setup(x => x.CreateCertificate(It.IsAny<byte[]>())).Returns(new X509Certificate2(GenerateCertificate().Export(X509ContentType.Cert)));
        _individualEnrollmentWrapperMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<Attestation>())).Returns(enrollment);


        _provisioningServiceClientWrapperMock.Setup(x => x.CreateOrUpdateIndividualEnrollmentAsync(It.IsAny<ProvisioningServiceClient>(), It.IsAny<IndividualEnrollment>())).ReturnsAsync(enrollment);
        await _target.ProvisionDeviceCertificateAsync(DEVICE_ID, new byte[100]);

        _deviceClientWrapperMock.Verify(x => x.SendAsync(It.IsAny<ServiceClient>(), It.IsAny<string>(), It.IsAny<Message>()), Times.Once);
    }

    [Test]
    public async Task ProvisionDeviceCertificateAsync_InvalidCertificateParameter_ThrowException()
    {
        var enrollment = new IndividualEnrollment("", new SymmetricKeyAttestation("", ""));
        _x509CertificateWrapperMock.Setup(x => x.CreateCertificate(It.IsAny<byte[]>())).Returns(new X509Certificate2(GenerateCertificate().Export(X509ContentType.Cert)));
        _individualEnrollmentWrapperMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<Attestation>())).Returns(enrollment);


        _provisioningServiceClientWrapperMock.Setup(x => x.CreateOrUpdateIndividualEnrollmentAsync(It.IsAny<ProvisioningServiceClient>(), It.IsAny<IndividualEnrollment>())).ReturnsAsync(enrollment);
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.ProvisionDeviceCertificateAsync(DEVICE_ID, null));

    }
        [Test]
    public async Task ProvisionDeviceCertificateAsync_InvalidDeviceIdParameter_ThrowException()
    {
        var enrollment = new IndividualEnrollment("", new SymmetricKeyAttestation("", ""));
        _x509CertificateWrapperMock.Setup(x => x.CreateCertificate(It.IsAny<byte[]>())).Returns(new X509Certificate2(GenerateCertificate().Export(X509ContentType.Cert)));
        _individualEnrollmentWrapperMock.Setup(x => x.Create(It.IsAny<string>(), It.IsAny<Attestation>())).Returns(enrollment);


        _provisioningServiceClientWrapperMock.Setup(x => x.CreateOrUpdateIndividualEnrollmentAsync(It.IsAny<ProvisioningServiceClient>(), It.IsAny<IndividualEnrollment>())).ReturnsAsync(enrollment);
        Assert.ThrowsAsync<ArgumentNullException>(async () => await _target.ProvisionDeviceCertificateAsync(null, new byte[100]));
    }

    private X509Certificate2 GenerateCertificate()
    {
        using (RSA rsa = RSA.Create(4096))
        {
            var request = new CertificateRequest(
                $"CN=test", rsa
                , HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            var certificate = request.CreateSelfSigned(
                DateTimeOffset.Now.AddDays(-1),
                DateTimeOffset.Now.AddDays(1));
            return certificate;

        }
    }
}
