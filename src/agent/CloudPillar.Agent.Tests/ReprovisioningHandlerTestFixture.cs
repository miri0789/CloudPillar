using System.Security.Cryptography.X509Certificates;
using System.Text;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
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
    private IReprovisioningHandler _target;
    private const string DEVICE_ID = "UnitTest";
    private const string SECRET_KEY = "secert";


    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILoggerHandler>();
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _x509CertificateWrapperMock = new Mock<IX509CertificateWrapper>();
        _dPSProvisioningDeviceClientHandlerMock = new Mock<IDPSProvisioningDeviceClientHandler>();
        _environmentsWrapperMock = new Mock<IEnvironmentsWrapper>();
        _d2CMessengerHandlerMock = new Mock<ID2CMessengerHandler>();

        _target = new ReprovisioningHandler(_deviceClientWrapperMock.Object,
        _x509CertificateWrapperMock.Object,
        _dPSProvisioningDeviceClientHandlerMock.Object,
        _environmentsWrapperMock.Object,
        _d2CMessengerHandlerMock.Object,
        _loggerMock.Object);
    }

    [Test]
    public async Task HandleRequestDeviceCertificateAsync_ValidMessage_GeneratesAndInstallsCertificate()
    {
        var authonticationKeys = new AuthonticationKeys()
        {
            DeviceId = DEVICE_ID,
            SecretKey = SECRET_KEY
        };

        var message = new RequestDeviceCertificateMessage
        {
            Data = Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(authonticationKeys))
        };

        _environmentsWrapperMock.Setup(x => x.certificateExpiredDays).Returns(365);

        // Set up the mock's behavior to generate a temporary certificate
        _x509CertificateWrapperMock.Setup(x => x.GetStore(StoreLocation.CurrentUser)).Returns(Mock.Of<X509Store>());
        x509CertificateWrapperMock.Setup(x => x.GetSecurityProvider(It.IsAny<X509Certificate2>()))
            .Returns(Mock.Of<IAuthenticationMethod>());

        var generatedCertificate = new X509Certificate2(); // A valid certificate
        _x509CertificateWrapperMock.Setup(x => x.GenerateCertificate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(generatedCertificate);

        // Act
        await _target.HandleRequestDeviceCertificateAsync(message, CancellationToken.None);


        _d2CMessengerHandlerMock.Verify(d2c => d2c.ProvisionDeviceCertificateEventAsync(), Times.Once)
         _d2CMessengerHandler.ProvisionDeviceCertificateEventAsync(certificate);
        _messageSubscriberMock.Verify(ms => ms.HandleDownloadMessageAsync(_downloadBlobChunkMessage), Times.Once);
        // Assert
        // You can add assertions here to ensure the expected behavior, e.g., check if certificate generation and installation is done correctly.
    }
}