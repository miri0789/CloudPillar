using System.Security.Cryptography.X509Certificates;
using CloudPillar.Agent.Handlers;
using CloudPillar.Agent.Wrappers;
using Moq;
using Shared.Entities.Authentication;
using Shared.Logger;

namespace CloudPillar.Agent.Tests;
[TestFixture]
public class X509DPSProvisioningDeviceClientHandlerTestFixture
{
    private Mock<ILoggerHandler> _loggerMock;
    private Mock<IDeviceClientWrapper> _deviceClientWrapperMock;
    private Mock<IX509CertificateWrapper> _x509CertificateWrapperMock;
    private IDPSProvisioningDeviceClientHandler _target;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILoggerHandler>();
        _deviceClientWrapperMock = new Mock<IDeviceClientWrapper>();
        _x509CertificateWrapperMock = new Mock<IX509CertificateWrapper>();
        _target = new X509DPSProvisioningDeviceClientHandler(_loggerMock.Object, _deviceClientWrapperMock.Object, _x509CertificateWrapperMock.Object);
    }

    [Test]
    public void GetCertificate_ValidCertificateExists_ReturnsCertificate()
    {
        // Create a collection of certificates containing a valid certificate
        // Create a collection of mock certificates
        // var certificates = new X509Certificate2Collection();

        // // Create a mock certificate and add it to the collection
        // var mockCertificate = new Mock<X509Certificate2>();
        // mockCertificate.Setup(cert => cert.Subject).Returns("CN=YourMockCertificate");
        // certificates.Add(mockCertificate.Object);

        // // Set up the mock's behavior to return the collection of mock certificates
        // _x509CertificateWrapperMock.Setup(x => x.GetStore(StoreLocation.CurrentUser)).Returns(certificates);

        var mockCertificate = new Mock<X509Certificate2>();
        //mockCertificate.Object.Subject = 
        //mockCertificate.Setup(cert => cert.Subject).Returns(ProvisioningConstants.CERTIFICATE_SUBJECT + CertificateConstants.CLOUD_PILLAR_SUBJECT + "test");

        // Set up the mock's behavior to return the mock certificate
        _x509CertificateWrapperMock.Setup(x => x.GetStore(StoreLocation.CurrentUser)).Returns(() =>
        {
            var mockStore = new X509Store(StoreLocation.CurrentUser);
            mockStore.Setup(store => store.Certificates).Returns(new X509Certificate2Collection(mockCertificate.Object));
            return mockStore.Object;
        });

        var target = _target.GetCertificate();

        Assert.IsNotNull(target);


    }

}